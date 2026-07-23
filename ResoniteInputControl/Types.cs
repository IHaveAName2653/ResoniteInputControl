using System.Collections.Generic;
using Elements.Core;
using FrooxEngine;

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
	public IInputNode<bool> LeftButton;
    public IInputNode<bool> RightButton;
	public int LeftIndex;
	public int RightIndex;
}

public struct ControllerState(bool move = true, bool turn = true, bool jump = true)
{
	public bool Move = move;
	public bool Turn = turn;
	public bool Jump = jump;
}

public struct StateData()
{
	public ControllerState Left = new(true);
	public ControllerState Right = new(true);
}