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
using System.Collections.Generic;
using System.Linq;

using Decider.Csp.BaseTypes;
using Decider.Csp.Integer;

namespace Decider.Csp.Global
{
	public class AllDifferentInteger : IConstraint
	{
		private readonly VariableInteger[] variableArray;
		private readonly IDomain<int>[] domainArray;
		private BipartiteGraph Graph { get; set; }
		private readonly CycleDetection cycleDetection;
		
		private IState<int> State { get; set; }
		private int Depth
		{
			get
			{
				if (this.State == null)
					this.State = this.variableArray[0].State;

				return this.State.Depth;
			}
		}

		public AllDifferentInteger(IEnumerable<VariableInteger> variables)
		{
			this.variableArray = variables.ToArray();
			this.domainArray = new IDomain<int>[this.variableArray.Length];
			this.cycleDetection = new CycleDetection();
		}

		void IConstraint.Check(out ConstraintOperationResult result)
		{
			for (var i = 0; i < this.variableArray.Length; ++i)
				this.domainArray[i] = variableArray[i].Domain;

			if (!FindMatching())
			{
				result = ConstraintOperationResult.Violated;
				return;
			}

			if (this.variableArray.Cast<IVariable<int>>().Any(variable => !variable.Instantiated()))
			{
				result = ConstraintOperationResult.Undecided;
				return;
			}

			result = ConstraintOperationResult.Satisfied;
		}

		private bool FindMatching()
		{
			return this.Graph.MaximalMatching() >= this.variableArray.Length;
		}

		void IConstraint.Propagate(out ConstraintOperationResult result)
		{
			this.Graph = new BipartiteGraph(this.variableArray);

			if (!FindMatching())
			{
				result = ConstraintOperationResult.Violated;
				return;
			}

			this.cycleDetection.Graph = this.Graph;
			this.cycleDetection.DetectCycle();
			
			result = ConstraintOperationResult.Undecided;
			foreach (var cycle in this.cycleDetection.StronglyConnectedComponents)
			{
				foreach (var node in cycle)
				{
					if (!(node is NodeVariable) || node == this.Graph.NullNode)
						continue;

					var variable = ((NodeVariable) node).Variable;
					foreach (var value in variable.Domain.Cast<int>().Where(value =>
						this.Graph.Values[value].CycleIndex != node.CycleIndex &&
						((NodeValue) this.Graph.Pair[node]).Value != value))
					{
						result = ConstraintOperationResult.Propagated;

						DomainOperationResult domainResult;
						((IVariable<int>) variable).Remove(value, this.Depth, out domainResult);
						
						if (domainResult != DomainOperationResult.EmptyDomain)
							continue;

						result = ConstraintOperationResult.Violated;
						return;
					}
				}
			}
		}

		bool IConstraint.StateChanged()
		{
			return this.variableArray.Where((t, i) => t.Domain != this.domainArray[i]).Any();
		}
	}
}
