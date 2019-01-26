/*
  Copyright © Iain McDonald 2010-2019
  
  This file is part of Decider.
*/
using System;
using System.Collections.Generic;

namespace Decider.Csp.BaseTypes
{
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
		int NumberOfSolutions { get; }

		void SetVariables(IEnumerable<IVariable<T>> variableList);
		void SetConstraints(IEnumerable<IConstraint> constraintList);
		void StartSearch(out StateOperationResult result);
		void StartSearch(out StateOperationResult result, out IList<IDictionary<string, IVariable<T>>> solutions);
		void StartSearch(out StateOperationResult result, IVariable<int> optimiseVar, out IDictionary<string, IVariable<int>> solution, int timeOut);
	}
}
