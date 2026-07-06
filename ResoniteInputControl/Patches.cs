using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;

namespace ResoniteInputControl;

public struct MoveInputData
{
	public VR_LocomotionDirection controller;
	public IInputNode<float2> leftAxis;
	public IInputNode<float2> rightAxis;
	public IInputNode<float> leftSpeed;
	public IInputNode<float> rightSpeed;
}

public struct TurnInputData
{
	public VR_LocomotionTurn controller;
	public IInputNode<float2> leftAxis;
	public IInputNode<float2> rightAxis;
}

public struct Turn3AxisInputData
{
	public VR_LocomotionThreeAxisTurn controller;
	public IInputNode<float2> leftAxis;
	public IInputNode<float2> rightAxis;
}

public struct JumpInputData
{
	public AnyInput controller;
	public List<IInputNode<bool>> inputs;
}

public struct StateData(bool leftMove = true, bool rightMove = true, bool leftTurn = true, bool rightTurn = true, bool leftJump = true, bool rightJump = true)
{
	public bool LeftMove = leftMove;
	public bool RightMove = rightMove;

	public bool LeftTurn = leftTurn;
	public bool RightTurn = rightTurn;

	public bool LeftJump = leftJump;
	public bool RightJump = rightJump;
}

[HarmonyPatch]
public static class Patches
{
	const string LeftMoveVariable = "User/Joystick.Left.Move";
	const string RightMoveVariable = "User/Joystick.Right.Move";

	const string LeftTurnVariable = "User/Joystick.Left.Turn";
	const string RightTurnVariable = "User/Joystick.Right.Turn";

	const string LeftJumpVariable = "User/Joystick.Left.Jump";
	const string RightJumpVariable = "User/Joystick.Right.Jump";

	static World? currentWorld = null;

	static NullInputNode<bool> nullJump = new(false);
	static NullInputNode<float> nullSpeed = new(0);
	static NullInputNode<float2> nullAxis = new(float2.Zero);

	public static Dictionary<World, StateData> currentStates = [];

	public static List<MoveInputData> storedMoveData = [];
	public static List<TurnInputData> storedTurnData = [];
	public static List<Turn3AxisInputData> storedTurn3AxisData = [];
	public static List<JumpInputData> storedJumpData = [];

	[HarmonyPatch(typeof(DualControllerBindingGenerator), "BindLocomotionDirection")]
	[HarmonyPostfix]
	public static void FindControllerMoveInputPatch(VR_LocomotionDirection __result)
	{
		storedMoveData.Add(new()
		{
			controller = __result,
			leftAxis = __result.LeftAxis,
			rightAxis = __result.RightAxis,
			leftSpeed = __result.LeftSpeed,
			rightSpeed = __result.RightSpeed,
		});
	}

	[HarmonyPatch(typeof(DualControllerBindingGenerator), "BindLocomotionTurn")]
	[HarmonyPostfix]
	public static void FindControllerTurnInputPatch(VR_LocomotionTurn __result)
	{
		storedTurnData.Add(new()
		{
			controller = __result,
			leftAxis = __result.LeftAxis,
			rightAxis = __result.RightAxis,
		});
	}

	[HarmonyPatch(typeof(DualControllerBindingGenerator), "BindThreeAxisTurn")]
	[HarmonyPostfix]
	public static void FindControllerTurn3AxisInputPatch(VR_LocomotionThreeAxisTurn __result)
	{
		storedTurn3AxisData.Add(new()
		{
			controller = __result,
			leftAxis = __result.LeftAxis,
			rightAxis = __result.RightAxis,
		});
	}

	[HarmonyPatch(typeof(DualControllerBindingGenerator), "BindJump")]
	[HarmonyPostfix]
	public static void FindControllerJumpInputPatch(AnyInput __result)
	{
		storedJumpData.Add(new()
		{
			controller = __result,
			inputs = [..__result.Inputs] // copy the values
		});
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(UserRoot), "OnStart")]
	public static void OnUserRootInitialize(UserRoot __instance)
	{
		if (!ResoniteInputControl.shouldBeActive.Value) return;
		if (__instance != __instance.LocalUserRoot) return;
		World world = __instance.World;
		if (world.IsUserspace()) return;
		Slot userRoot = __instance.Slot;

		var leftMoveComp = userRoot.AttachComponent<DynamicValueVariable<bool>>();
		leftMoveComp.VariableName.Value = LeftMoveVariable;
		leftMoveComp.Value.Value = true;
		leftMoveComp.Value.OnValueChange += LeftMoveChangedEvent;

		var rightMoveComp = userRoot.AttachComponent<DynamicValueVariable<bool>>();
		rightMoveComp.VariableName.Value = RightMoveVariable;
		rightMoveComp.Value.Value = true;
		rightMoveComp.Value.OnValueChange += RightMoveChangedEvent;


		var leftTurnComp = userRoot.AttachComponent<DynamicValueVariable<bool>>();
		leftTurnComp.VariableName.Value = LeftTurnVariable;
		leftTurnComp.Value.Value = true;
		leftTurnComp.Value.OnValueChange += LeftTurnChangedEvent;

		var rightTurnComp = userRoot.AttachComponent<DynamicValueVariable<bool>>();
		rightTurnComp.VariableName.Value = RightTurnVariable;
		rightTurnComp.Value.Value = true;
		rightTurnComp.Value.OnValueChange += RightTurnChangedEvent;


		var leftJumpComp = userRoot.AttachComponent<DynamicValueVariable<bool>>();
		leftJumpComp.VariableName.Value = LeftJumpVariable;
		leftJumpComp.Value.Value = true;
		leftJumpComp.Value.OnValueChange += LeftJumpChangedEvent;

		var rightJumpComp = userRoot.AttachComponent<DynamicValueVariable<bool>>();
		rightJumpComp.VariableName.Value = RightJumpVariable;
		rightJumpComp.Value.Value = true;
		rightJumpComp.Value.OnValueChange += RightJumpChangedEvent;


		UpdateForWorld(world);
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(WorldManager), "FocusWorld")]
	public static void OnWorldFocus(World world)
	{
		UpdateForWorld(world);
	}

	

	public static void LeftMoveChangedEvent(SyncField<bool> field)
	{
		bool value = field.Value;
		if (currentStates.TryGetValue(field.World, out var state))
		{
			state.LeftMove = value;
			currentStates[field.World] = state;
		}
		else
		{
			currentStates.Add(field.World, new(leftMove: value));
		}
		RegisterControllerModifications();
	}
	public static void RightMoveChangedEvent(SyncField<bool> field)
	{
		bool value = field.Value;
		if (currentStates.TryGetValue(field.World, out var state))
		{
			state.RightMove = value;
			currentStates[field.World] = state;
		}
		else
		{
			currentStates.Add(field.World, new(rightMove: value));
		}
		RegisterControllerModifications();
	}

	public static void LeftTurnChangedEvent(SyncField<bool> field)
	{
		bool value = field.Value;
		if (currentStates.TryGetValue(field.World, out var state))
		{
			state.LeftTurn = value;
			currentStates[field.World] = state;
		}
		else
		{
			currentStates.Add(field.World, new(leftTurn: value));
		}
		RegisterControllerModifications();
	}
	public static void RightTurnChangedEvent(SyncField<bool> field)
	{
		bool value = field.Value;
		if (currentStates.TryGetValue(field.World, out var state))
		{
			state.RightTurn = value;
			currentStates[field.World] = state;
		}
		else
		{
			currentStates.Add(field.World, new(rightTurn: value));
		}
		RegisterControllerModifications();
	}

	public static void LeftJumpChangedEvent(SyncField<bool> field)
	{
		bool value = field.Value;
		if (currentStates.TryGetValue(field.World, out var state))
		{
			state.LeftJump = value;
			currentStates[field.World] = state;
		}
		else
		{
			currentStates.Add(field.World, new(leftJump: value));
		}
		RegisterControllerModifications();
	}
	public static void RightJumpChangedEvent(SyncField<bool> field)
	{
		bool value = field.Value;
		if (currentStates.TryGetValue(field.World, out var state))
		{
			state.RightJump = value;
			currentStates[field.World] = state;
		}
		else
		{
			currentStates.Add(field.World, new(rightJump: value));
		}
		RegisterControllerModifications();
	}

	public static void RegisterControllerModifications()
	{
		if (currentWorld == null) return;
		bool leftMove = true;
		bool rightMove = true;
		bool leftTurn = true;
		bool rightTurn = true;
		bool leftJump = true;
		bool rightJump = true;
		if (currentStates.TryGetValue(currentWorld, out var vals))
		{
			leftMove = vals.LeftMove;
			rightMove = vals.RightMove;
			leftTurn = vals.LeftTurn;
			rightTurn = vals.RightTurn;
			leftJump = vals.LeftJump;
			rightJump = vals.RightJump;
		}
		storedMoveData.ForEach(v =>
		{
			var controller = v.controller;

			controller.LeftAxis = leftMove ? v.leftAxis : nullAxis;
			controller.LeftSpeed = leftMove ? v.leftSpeed : nullSpeed;
			controller.RightAxis = rightMove ? v.rightAxis : nullAxis;
			controller.RightSpeed = rightMove ? v.rightSpeed : nullSpeed;
		});

		storedTurnData.ForEach(v =>
		{
			var controller = v.controller;

			controller.LeftAxis = leftTurn ? v.leftAxis : nullAxis;
			controller.RightAxis = rightTurn ? v.rightAxis : nullAxis;
		});

		storedTurn3AxisData.ForEach(v =>
		{
			var controller = v.controller;

			controller.LeftAxis = leftTurn ? v.leftAxis : nullAxis;
			controller.RightAxis = rightTurn ? v.rightAxis : nullAxis;
		});


		storedJumpData.ForEach(v =>
		{
			var controller = v.controller;

			controller.Inputs.RemoveAt(0);
			controller.Inputs.RemoveAt(0);
			controller.Inputs.Add(leftJump ? v.inputs[0] : nullJump);
			controller.Inputs.Add(rightJump ? v.inputs[1] : nullJump);
		});
	}

	public static void UpdateForWorld(World world)
	{
		if (!ResoniteInputControl.shouldBeActive.Value) return;

		currentWorld = world;

		RegisterControllerModifications();
	}
}
