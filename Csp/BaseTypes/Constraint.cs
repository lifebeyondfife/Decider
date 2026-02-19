/*
  Copyright © Iain McDonald 2010-2026

  This file is part of Decider.
*/
using System;
using System.Collections.Generic;

namespace Decider.Csp.BaseTypes;

[Flags]
public enum ConstraintOperationResult
{
	Satisfied = 0x1,
	Violated = 0x2,
	Undecided = 0x4,
	Propagated = 0x8
}

public interface IConstraint
{
	void Check(out ConstraintOperationResult result);
	void Propagate(out ConstraintOperationResult result);
	bool StateChanged();
}

public interface IConstraint<T> : IConstraint
{
	IReadOnlyList<IVariable<T>> Variables { get; }
}

public interface IBacktrackableConstraint : IConstraint
{
	void OnBacktrack(int toDepth);
}

public record struct BoundReason(int VariableIndex, bool IsLowerBound, int BoundValue);

public interface IReasoningConstraint : IConstraint
{
	bool GenerateReasons { get; set; }
	IList<BoundReason>? LastReason { get; }
}
