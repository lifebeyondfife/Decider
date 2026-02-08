/*
  Copyright © Iain McDonald 2010-2022

  This file is part of Decider.
*/
namespace Decider.Csp.BaseTypes;

public struct Bounds<T>
{
	public T LowerBound;
	public T UpperBound;

	public Bounds(T lowerBound, T upperBound)
	{
		this.LowerBound = lowerBound;
		this.UpperBound = upperBound;
	}
}

public abstract class Expression<T>
{
	public abstract T Value { get; }
	public abstract bool IsBound { get; }
	public abstract Bounds<T> GetUpdatedBounds();
	public abstract void Propagate(Bounds<T> enforceBounds, out ConstraintOperationResult result);
	public Bounds<T> Bounds;
}
