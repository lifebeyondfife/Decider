/*
  Copyright © Iain McDonald 2010-2026

  This file is part of Decider.
*/
using System;
using System.Collections.Generic;
using System.Threading;

namespace Decider.Csp.BaseTypes;

public enum StateOperationResult
{
	Solved,
	Unsatisfiable,
	Cancelled
}

public interface IState<T>
{
	int Depth { get; }
	TimeSpan Runtime { get; }
	int Backtracks { get; }
	IList<IDictionary<string, IVariable<T>>> Solutions { get; }
	IDictionary<string, IVariable<T>>? OptimalSolution { get; }
	IList<IVariable<T>> Variables { get; }

	Action<double>? OnProgress { get; set; }
	TimeSpan ProgressInterval { get; set; }

	void SetVariables(IEnumerable<IVariable<T>> variableList);
	void SetConstraints(IEnumerable<IConstraint> constraintList);

	StateOperationResult Search();
	StateOperationResult Search(IVariable<int> optimiseVariable, CancellationToken cancellationToken = default);
	StateOperationResult SearchAllSolutions();
}
