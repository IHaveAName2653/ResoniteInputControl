using FrooxEngine;

namespace ResoniteInputControl;

public class NullInputNode<T>(T defaultValue) : IInputNode<T> where T : struct
{
	readonly T value = defaultValue;

	public T? Evaluate(in InputEvaluationContext context)
	{
		return value;
	}

	public void ResetNode()
	{
	}
}
