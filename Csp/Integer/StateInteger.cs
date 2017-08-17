/*
  Copyright © Iain McDonald 2010-2017
  
  This file is part of Decider.
*/
using System;
using System.Collections.Generic;
using System.Linq;

using Decider.Csp.BaseTypes;

namespace Decider.Csp.Integer
{
	public class StateInteger : IState<int>
	{
		public List<IConstraint> ConstraintList { get; private set; }
		public IVariable<int>[] VariableList { get; private set; }

		public int Depth { get; private set; }
		public int Backtracks { get; private set; }
		public TimeSpan Runtime { get; private set; }
		public int NumberOfSolutions { get; private set; }

		private IVariable<int>[] LastSolution { get; set; }

		public StateInteger(IEnumerable<IVariable<int>> variables, IEnumerable<IConstraint> constraints)
		{
			((IState<int>) this).SetVariables(variables);
			((IState<int>) this).SetConstraints(constraints);
			this.Depth = 0;
			this.Backtracks = 0;
			this.Runtime = new TimeSpan(0);
		}

		void IState<int>.SetVariables(IEnumerable<IVariable<int>> variableList)
		{
			this.VariableList = variableList.ToArray();

			foreach (var variable in this.VariableList)
				variable.SetState(this);
		}

		void IState<int>.SetConstraints(IEnumerable<IConstraint> constraintList)
		{
			this.ConstraintList = constraintList.ToList();
		}

		void IState<int>.StartSearch(out StateOperationResult searchResult)
		{
			var unassignedVariables = this.LastSolution == null
				? new LinkedList<IVariable<int>>(this.VariableList)
				: new LinkedList<IVariable<int>>();
			var instantiatedVariables = this.LastSolution ?? new IVariable<int>[this.VariableList.Length];
			var startTime = DateTime.Now;

			try
			{
				if (this.Depth == instantiatedVariables.Length)
				{
					--this.Depth;
					Backtrack(unassignedVariables, instantiatedVariables);
					++this.Depth;
				}
				else if (ConstraintsViolated())
				{
					throw new DeciderException("No solution found.");
				}

				Search(out searchResult, unassignedVariables, instantiatedVariables, startTime);
			}
			catch (DeciderException)
			{
				searchResult = StateOperationResult.Unsatisfiable;
				this.Runtime += DateTime.Now - startTime;
			}
		}

		void IState<int>.StartSearch(out StateOperationResult searchResult,
			out IList<IDictionary<string,IVariable<int>>> solutions)
		{
			var unassignedVariables = this.LastSolution == null
				? new LinkedList<IVariable<int>>(this.VariableList)
				: new LinkedList<IVariable<int>>();
			var instantiatedVariables = this.LastSolution ?? new IVariable<int>[this.VariableList.Length];
			var startTime = DateTime.Now;

			searchResult = StateOperationResult.Unsatisfiable;
			var solutionsList = new List<IDictionary<string, IVariable<int>>>();

			try
			{
				while (true)
				{
					if (this.Depth == instantiatedVariables.Length)
					{
						--this.Depth;
						Backtrack(unassignedVariables, instantiatedVariables);
						++this.Depth;
					}
					else if (ConstraintsViolated())
					{
						throw new DeciderException("No solution found.");
					}

					startTime = DateTime.Now;
					Search(out searchResult, unassignedVariables, instantiatedVariables, startTime);

					solutionsList.Add(this.LastSolution.Select(v => v.Clone())
					    .Cast<IVariable<int>>()
					    .Select(v => new KeyValuePair<string, IVariable<int>>(v.Name, v))
						.OrderBy(kvp => kvp.Key)
						.ToDictionary(k => k.Key, v => v.Value));
				}
			}
			catch (DeciderException)
			{
				this.Runtime += DateTime.Now - startTime;
			}

			solutions = solutionsList;
		}

		void IState<int>.StartSearch(out StateOperationResult searchResult, IVariable<int> optimiseVar, out IDictionary<string, IVariable<int>> solution, int timeOut)
		{
			var unassignedVariables = this.LastSolution == null
				? new LinkedList<IVariable<int>>(this.VariableList)
				: new LinkedList<IVariable<int>>();
			var instantiatedVariables = this.LastSolution ?? new IVariable<int>[this.VariableList.Length];
			var startTime = DateTime.Now;

			solution = new Dictionary<string, IVariable<int>>();
			searchResult = StateOperationResult.Unsatisfiable;

			this.ConstraintList.Add(new ConstraintInteger((VariableInteger) optimiseVar > Int32.MinValue));

			try
			{
				while (true)
				{
					if (this.Depth == instantiatedVariables.Length)
					{
						--this.Depth;
						Backtrack(unassignedVariables, instantiatedVariables);
						++this.Depth;
					}
					else if (ConstraintsViolated())
					{
						throw new DeciderException("No solution found.");
					}

					startTime = DateTime.Now;
					Search(out searchResult, unassignedVariables, instantiatedVariables, startTime, timeOut);

					this.ConstraintList.RemoveAt(this.ConstraintList.Count - 1);
					this.ConstraintList.Add(new ConstraintInteger((VariableInteger) optimiseVar > optimiseVar.InstantiatedValue));

					// Console.WriteLine("Optimised Value: {0} ({1}s)", optimiseVar.InstantiatedValue, DateTime.Now - startTime);

					solution = this.LastSolution.Select(v => v.Clone())
						.Cast<IVariable<int>>()
						.Select(v => new KeyValuePair<string, IVariable<int>>(v.Name, v))
						.OrderBy(kvp => kvp.Key)
						.ToDictionary(k => k.Key, v => v.Value);
				}
			}
			catch (DeciderException)
			{
				this.Runtime += DateTime.Now - startTime;
			}
		}

		private void Search(out StateOperationResult searchResult, LinkedList<IVariable<int>> unassignedVariables,
			IList<IVariable<int>> instantiatedVariables, DateTime startTime, int timeOut = Int32.MaxValue)
		{
			while (true)
			{
				if (this.Depth == this.VariableList.Length)
				{
					searchResult = StateOperationResult.Solved;
					this.Runtime += DateTime.Now - startTime;

					this.LastSolution = instantiatedVariables.ToArray();
					++this.NumberOfSolutions;

					return;
				}

				DomainOperationResult instantiateResult;
				instantiatedVariables[this.Depth] = GetMostConstrainedVariable(unassignedVariables);
				instantiatedVariables[this.Depth].Instantiate(this.Depth, out instantiateResult);

				if (ConstraintsViolated() || unassignedVariables.Any(v => v.Size() == 0))
				{
					if ((DateTime.Now - startTime).Seconds > timeOut)
						throw new DeciderException();

					Backtrack(unassignedVariables, instantiatedVariables);
				}

				++this.Depth;
			}
		}

		private void Backtrack(LinkedList<IVariable<int>> unassignedVariables, IList<IVariable<int>> instantiatedVariables)
		{
			DomainOperationResult removeResult;
			do
			{
				if (this.Depth < 0)
					throw new DeciderException("No solution found.");

				unassignedVariables.AddFirst(instantiatedVariables[this.Depth]);
				BackTrackVariable(instantiatedVariables[this.Depth], out removeResult);
			} while (removeResult == DomainOperationResult.EmptyDomain);
		}

		private bool ConstraintsViolated()
		{
			foreach (var constraint in this.ConstraintList.Where(constraint => constraint.StateChanged()))
			{
				ConstraintOperationResult result;
				constraint.Propagate(out result);
				if ((result & ConstraintOperationResult.Violated) == ConstraintOperationResult.Violated)
					return true;

				constraint.Check(out result);
				if ((result & ConstraintOperationResult.Violated) == ConstraintOperationResult.Violated)
					return true;
			}

			return false;
		}

		private void BackTrackVariable(IVariable<int> variablePrune, out DomainOperationResult result)
		{
			++this.Backtracks;
			var value = variablePrune.InstantiatedValue;

			foreach (var variable in this.VariableList)
				variable.Backtrack(this.Depth);
			--this.Depth;

			variablePrune.Remove(value, this.Depth, out result);
		}

		private static IVariable<int> GetMostConstrainedVariable(LinkedList<IVariable<int>> list)
		{
			var temp = list.First;
			var node = list.First;

			while (node != null)
			{
				if (node.Value.Size() < temp.Value.Size())
					temp = node;

				if (temp.Value.Size() == 1)
					break;

				node = node.Next;
			}
			list.Remove(temp);

			return temp.Value;
		}

		private readonly Random ran = new Random();
		private IVariable<int> GetRandomVariable(LinkedList<IVariable<int>> list)
		{
			var index = ran.Next(0, list.Count - 1);
			var node = list.First;
			while (--index >= 0)
				node = node.Next;
			list.Remove(node);
			return node.Value;
		}

		private IVariable<int> GetFirstVariable(LinkedList<IVariable<int>> list)
		{
			var first = list.First;
			list.Remove(first);
			return first.Value;
		}

		private IVariable<int> GetLastVariable(LinkedList<IVariable<int>> list)
		{
			var last = list.Last;
			list.Remove(last);
			return last.Value;
		}
	}
}
