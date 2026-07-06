using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;

namespace ResoniteInputControl;

[HarmonyPatch]
public static class Patches
{
	const string VariableBase = "User/InputControl.{0}.{1}";

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

	static void InitVariable(Slot root, string VariableName, SyncFieldEvent<bool> OnChange)
	{
		var component = root.AttachComponent<DynamicValueVariable<bool>>();
		component.VariableName.Value = VariableName;
		component.Value.Value = true;
		component.Value.OnValueChange += OnChange;
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

		InitVariable(userRoot, string.Format(VariableBase, "Left", "Move"), LeftMoveChangedEvent);
		InitVariable(userRoot, string.Format(VariableBase, "Right", "Move"), RightMoveChangedEvent);

		InitVariable(userRoot, string.Format(VariableBase, "Left", "Turn"), LeftTurnChangedEvent);
		InitVariable(userRoot, string.Format(VariableBase, "Right", "Turn"), RightTurnChangedEvent);

		InitVariable(userRoot, string.Format(VariableBase, "Left", "Jump"), LeftJumpChangedEvent);
		InitVariable(userRoot, string.Format(VariableBase, "Right", "Jump"), RightJumpChangedEvent);


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
		if (!currentStates.TryGetValue(field.World, out var state)) state = new();
		state.Left.Move = value;
		if (currentStates.ContainsKey(field.World)) currentStates[field.World] = state;
		else currentStates.Add(field.World, state);
		RegisterControllerModifications();
	}
	public static void RightMoveChangedEvent(SyncField<bool> field)
	{
		bool value = field.Value;
		if (!currentStates.TryGetValue(field.World, out var state)) state = new();
		state.Right.Move = value;
		if (currentStates.ContainsKey(field.World)) currentStates[field.World] = state;
		else currentStates.Add(field.World, state);
		RegisterControllerModifications();
	}

	public static void LeftTurnChangedEvent(SyncField<bool> field)
	{
		bool value = field.Value;
		if (!currentStates.TryGetValue(field.World, out var state)) state = new();
		state.Left.Turn = value;
		if (currentStates.ContainsKey(field.World)) currentStates[field.World] = state;
		else currentStates.Add(field.World, state);
		RegisterControllerModifications();
	}
	public static void RightTurnChangedEvent(SyncField<bool> field)
	{
		bool value = field.Value;
		if (!currentStates.TryGetValue(field.World, out var state)) state = new();
		state.Right.Turn = value;
		if (currentStates.ContainsKey(field.World)) currentStates[field.World] = state;
		else currentStates.Add(field.World, state);
		RegisterControllerModifications();
	}

	public static void LeftJumpChangedEvent(SyncField<bool> field)
	{
		bool value = field.Value;
		if (!currentStates.TryGetValue(field.World, out var state)) state = new();
		state.Left.Jump = value;
		if (currentStates.ContainsKey(field.World)) currentStates[field.World] = state;
		else currentStates.Add(field.World, state);
		RegisterControllerModifications();
	}
	public static void RightJumpChangedEvent(SyncField<bool> field)
	{
		bool value = field.Value;
		if (!currentStates.TryGetValue(field.World, out var state)) state = new();
		state.Right.Jump = value;
		if (currentStates.ContainsKey(field.World)) currentStates[field.World] = state;
		else currentStates.Add(field.World, state);
		RegisterControllerModifications();
	}

	public static void RegisterControllerModifications()
	{
		if (currentWorld == null) return;

		if (!currentStates.TryGetValue(currentWorld, out var vals)) vals = new();

		storedMoveData.ForEach(v =>
		{
			var controller = v.controller;

			controller.LeftAxis = vals.Left.Move ? v.leftAxis : nullAxis;
			controller.LeftSpeed = vals.Left.Move ? v.leftSpeed : nullSpeed;
			controller.RightAxis = vals.Right.Move ? v.rightAxis : nullAxis;
			controller.RightSpeed = vals.Right.Move ? v.rightSpeed : nullSpeed;
		});

		storedTurnData.ForEach(v =>
		{
			var controller = v.controller;

			controller.LeftAxis = vals.Left.Turn ? v.leftAxis : nullAxis;
			controller.RightAxis = vals.Right.Turn ? v.rightAxis : nullAxis;
		});

		storedTurn3AxisData.ForEach(v =>
		{
			var controller = v.controller;

			controller.LeftAxis = vals.Left.Turn ? v.leftAxis : nullAxis;
			controller.RightAxis = vals.Right.Turn ? v.rightAxis : nullAxis;
		});


		storedJumpData.ForEach(v =>
		{
			var controller = v.controller;

			controller.Inputs.RemoveAt(0);
			controller.Inputs.RemoveAt(0);
			controller.Inputs.Add(vals.Left.Jump ? v.inputs[0] : nullJump);
			controller.Inputs.Add(vals.Right.Jump ? v.inputs[1] : nullJump);
		});
	}

	public static void UpdateForWorld(World world)
	{
		if (!ResoniteInputControl.shouldBeActive.Value) return;

		currentWorld = world;

		RegisterControllerModifications();
	}
}
