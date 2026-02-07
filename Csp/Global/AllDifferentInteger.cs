/*
  Copyright © Iain McDonald 2010-2026
  
  This file is part of Decider.
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
		private readonly int[] generationArray;
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
			this.generationArray = new int[this.variableArray.Length];
			this.cycleDetection = new CycleDetection();
		}

		public void Check(out ConstraintOperationResult result)
		{
			for (var i = 0; i < this.variableArray.Length; ++i)
				this.generationArray[i] = variableArray[i].Generation;

			if (!FindMatching())
			{
				result = ConstraintOperationResult.Violated;
				return;
			}

			if (this.variableArray.Any(variable => !variable.Instantiated()))
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

		public void Propagate(out ConstraintOperationResult result)
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
					foreach (var value in variable.Domain.Where(value =>
						this.Graph.Values[value].CycleIndex != node.CycleIndex &&
						((NodeValue) this.Graph.Pair[node]).Value != value))
					{
						result = ConstraintOperationResult.Propagated;

						variable.Remove(value, this.Depth, out DomainOperationResult domainResult);

						if (domainResult != DomainOperationResult.EmptyDomain)
							continue;

						result = ConstraintOperationResult.Violated;
						return;
					}
				}
			}
		}

		public bool StateChanged()
		{
			return this.variableArray.Where((t, i) => t.Generation != this.generationArray[i]).Any();
		}
	}
}
