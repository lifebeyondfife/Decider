/*
  Copyright © Iain McDonald 2010-2022

  This file is part of Decider.
*/
using System;
using System.Collections.Generic;

namespace Decider.Csp.BaseTypes;

public enum StateOperationResult
{
	Solved,
	Unsatisfiable,
	TimedOut
}

public interface IState<T>
{
	int Depth { get; }
	TimeSpan Runtime { get; }
	int Backtracks { get; }
	IList<IDictionary<string, IVariable<T>>> Solutions { get; }
	IDictionary<string, IVariable<T>> OptimalSolution { get; }
	IList<IVariable<T>> Variables { get; }

	void SetVariables(IEnumerable<IVariable<T>> variableList);
	void SetConstraints(IEnumerable<IConstraint> constraintList);

	StateOperationResult Search();
	StateOperationResult Search(IVariable<int> optimiseVariable, int timeOut = Int32.MaxValue);
	StateOperationResult SearchAllSolutions();
}
