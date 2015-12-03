/*
  Copyright © Iain McDonald 2010-2013
  
  This file is part of Decider.

	Decider is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	Decider is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with Decider.  If not, see <http://www.gnu.org/licenses/>.
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
