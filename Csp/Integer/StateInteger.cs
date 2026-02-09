/*
  Copyright © Iain McDonald 2010-2026
  
  This file is part of Decider.
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Decider.Csp.BaseTypes;

namespace Decider.Csp.Integer;

public class StateInteger : IState<int>
{
	public IList<IConstraint> Constraints { get; private set; } = new List<IConstraint>();
	private IList<IBacktrackableConstraint> BacktrackableConstraints { get; set; } = new List<IBacktrackableConstraint>();
	public IList<IVariable<int>> Variables { get; private set; } = new List<IVariable<int>>();

	public int Depth { get; private set; }
	public int Backtracks { get; private set; }
	public TimeSpan Runtime { get; private set; }
	public IDictionary<string, IVariable<int>>? OptimalSolution { get; private set; }
	public IList<IDictionary<string, IVariable<int>>> Solutions { get; private set; }

	public Action<double>? OnProgress { get; set; }
	public TimeSpan ProgressInterval { get; set; }

	private IVariable<int>[]? LastSolution { get; set; }

	internal DomainTrail Trail { get; private set; }

	private IList<int> BranchFactor { get; set; } = new List<int>();
	private IList<int> Explored { get; set; } = new List<int>();
	private TimeSpan LastProgressReport { get; set; }

	public StateInteger(IEnumerable<IVariable<int>> variables, IEnumerable<IConstraint> constraints)
	{
		SetVariables(variables);
		SetConstraints(constraints);
		this.Depth = 0;
		this.Backtracks = 0;
		this.Runtime = new TimeSpan(0);
		this.Solutions = new List<IDictionary<string, IVariable<int>>>();

		this.ProgressInterval = TimeSpan.FromSeconds(1);
		this.LastProgressReport = TimeSpan.Zero;

		this.Trail = new DomainTrail(this.Variables.Count, this.Variables.Count * 10000);

		this.BranchFactor = new int[this.Variables.Count];
		this.Explored = new int[this.Variables.Count];

		for (var i = 0; i < this.Variables.Count; ++i)
			((VariableInteger)this.Variables[i]).SetVariableId(i);
	}

	public void SetVariables(IEnumerable<IVariable<int>> variableList)
	{
		this.Variables = variableList.ToList();

		foreach (var variable in this.Variables)
			variable.SetState(this);
	}

	public void SetConstraints(IEnumerable<IConstraint> constraints)
	{
		this.Constraints = constraints?.ToList() ?? new List<IConstraint>();
		this.BacktrackableConstraints = this.Constraints.OfType<IBacktrackableConstraint>().ToList();
	}

	public StateOperationResult Search()
	{
		var unassignedVariables = this.LastSolution == null
			? new LinkedList<IVariable<int>>(this.Variables)
			: new LinkedList<IVariable<int>>();
		var instantiatedVariables = this.LastSolution ?? new IVariable<int>[this.Variables.Count];
		var stopwatch = Stopwatch.StartNew();
		var searchResult = StateOperationResult.Unsatisfiable;

		if (this.Depth == instantiatedVariables.Length)
		{
			--this.Depth;
			Backtrack(unassignedVariables, instantiatedVariables);
			++this.Depth;
		}
		else if (ConstraintsViolated())
		{
			this.Runtime += stopwatch.Elapsed;
			stopwatch.Stop();
			return searchResult;
		}

		if (Search(out searchResult, unassignedVariables, instantiatedVariables, ref stopwatch))
			this.Solutions.Add(CloneLastSolution());

		this.Runtime += stopwatch.Elapsed;
		stopwatch.Stop();
		return searchResult;
	}

	public StateOperationResult Search(IVariable<int> optimiseVar, CancellationToken cancellationToken = default)
	{
		var unassignedVariables = this.LastSolution == null
			? new LinkedList<IVariable<int>>(this.Variables)
			: new LinkedList<IVariable<int>>();
		var instantiatedVariables = this.LastSolution ?? new IVariable<int>[this.Variables.Count];
		var stopwatch = Stopwatch.StartNew();
		var searchResult = StateOperationResult.Unsatisfiable;

		this.Constraints.Add(new ConstraintInteger((VariableInteger) optimiseVar < Int32.MaxValue));

		while (true)
		{
			if (this.Depth == instantiatedVariables.Length)
			{
				--this.Depth;
				Backtrack(unassignedVariables, instantiatedVariables);
				++this.Depth;
			}
			else if (ConstraintsViolated())
				break;

			if (Search(out searchResult, unassignedVariables, instantiatedVariables, ref stopwatch, cancellationToken))
			{
				this.Constraints.RemoveAt(this.Constraints.Count - 1);
				this.Constraints.Add(new ConstraintInteger((VariableInteger) optimiseVar < optimiseVar.InstantiatedValue));
				this.OptimalSolution = CloneLastSolution();
			}
			else if (searchResult == StateOperationResult.Cancelled)
				break;
		}

		if (this.LastSolution != null && searchResult == StateOperationResult.Unsatisfiable)
			searchResult = StateOperationResult.Solved;

		this.Runtime += stopwatch.Elapsed;
		stopwatch.Stop();
		return searchResult;
	}

	public StateOperationResult SearchAllSolutions()
	{
		var unassignedVariables = this.LastSolution == null
			? new LinkedList<IVariable<int>>(this.Variables)
			: new LinkedList<IVariable<int>>();
		var instantiatedVariables = this.LastSolution ?? new IVariable<int>[this.Variables.Count];
		var stopwatch = Stopwatch.StartNew();

		var searchResult = StateOperationResult.Unsatisfiable;

		while (true)
		{
			if (this.Depth == -1)
				break;

			if (this.Depth == instantiatedVariables.Length)
			{
				--this.Depth;
				Backtrack(unassignedVariables, instantiatedVariables);
				++this.Depth;
			}
			else if (ConstraintsViolated())
			{
				this.Runtime += stopwatch.Elapsed;
				stopwatch.Stop();
				break;
			}

			if (Search(out searchResult, unassignedVariables, instantiatedVariables, ref stopwatch))
				this.Solutions.Add(CloneLastSolution());
		}

		this.Runtime += stopwatch.Elapsed;
		stopwatch.Stop();

		this.OnProgress?.Invoke(1.0);

		return Solutions.Any() ? StateOperationResult.Solved : StateOperationResult.Unsatisfiable;
	}

	private IDictionary<string, IVariable<int>> CloneLastSolution()
	{
		return this.LastSolution!.Select(v => v.Clone())
			.Cast<IVariable<int>>()
			.Select(v => new KeyValuePair<string, IVariable<int>>(v.Name, v))
			.OrderBy(kvp => kvp.Key)
			.ToDictionary(k => k.Key, v => v.Value);
	}

	private bool Search(out StateOperationResult searchResult, LinkedList<IVariable<int>> unassignedVariables,
		IList<IVariable<int>> instantiatedVariables, ref Stopwatch stopwatch, CancellationToken cancellationToken = default)
	{
		searchResult = StateOperationResult.Unsatisfiable;
		if (unassignedVariables.Any(x => x.Size() == 0))
		{
			this.Depth = -1;
			return false;
		}

		while (true)
		{
			if (this.Depth == this.Variables.Count)
			{
				searchResult = StateOperationResult.Solved;
				this.Runtime += stopwatch.Elapsed;
				stopwatch = Stopwatch.StartNew();

				this.LastSolution = instantiatedVariables.ToArray();

				return true;
			}

			instantiatedVariables[this.Depth] = GetMostConstrainedVariable(unassignedVariables);

			if (this.BranchFactor[this.Depth] == 0)
			{
				this.BranchFactor[this.Depth] = instantiatedVariables[this.Depth].Size();
				this.Explored[this.Depth] = 0;

				for (var i = this.Depth + 1; i < this.Explored.Count; ++i)
				{
					this.BranchFactor[i] = 0;
					this.Explored[i] = 0;
				}
			}

			instantiatedVariables[this.Depth].Instantiate(this.Depth, out DomainOperationResult instantiateResult);

			if (instantiateResult != DomainOperationResult.InstantiateSuccessful)
				return false;

			if (ConstraintsViolated() || unassignedVariables.Any(v => v.Size() == 0))
			{
				if (!Backtrack(unassignedVariables, instantiatedVariables))
					return false;
			}

			if (cancellationToken.IsCancellationRequested)
			{
				searchResult = StateOperationResult.Cancelled;
				return false;
			}

			var currentRuntime = this.Runtime + stopwatch.Elapsed;
			if (this.OnProgress != null && currentRuntime - this.LastProgressReport >= this.ProgressInterval)
			{
				this.OnProgress(ComputeProgress());
				this.LastProgressReport = currentRuntime;
			}

			++this.Depth;
		}
	}

	private bool Backtrack(LinkedList<IVariable<int>> unassignedVariables, IList<IVariable<int>> instantiatedVariables)
	{
		DomainOperationResult removeResult;
		do
		{
			if (this.Depth < 0)
				return false;

			unassignedVariables.AddFirst(instantiatedVariables[this.Depth]);
			BackTrackVariable(instantiatedVariables[this.Depth], out removeResult);

			if (removeResult == DomainOperationResult.EmptyDomain)
				this.BranchFactor[this.Depth + 1] = 0;
		} while (removeResult == DomainOperationResult.EmptyDomain);

		return true;
	}

	private bool ConstraintsViolated()
	{
		for (var i = 0; i < this.Constraints.Count; ++i)
		{
			var constraint = this.Constraints[i];
			if (!constraint.StateChanged())
				continue;

			constraint.Propagate(out ConstraintOperationResult result);
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

		foreach (var variable in this.Variables)
			variable.Backtrack(this.Depth);

		--this.Depth;
		++this.Explored[this.Depth + 1];

		this.Trail.Backtrack(this.Depth, this.Variables);

		foreach (var backtrackableConstraint in this.BacktrackableConstraints)
			backtrackableConstraint.OnBacktrack(this.Depth);

		variablePrune.Remove(value, this.Depth, out result);
	}

	private static IVariable<int> GetMostConstrainedVariable(LinkedList<IVariable<int>> list)
	{
		var temp = list.First;
		var node = list.First;

		while (node != null)
		{
			if (node.Value.Size() < temp!.Value.Size())
				temp = node;

			if (temp.Value.Size() == 1)
				break;

			node = node.Next;
		}
		list.Remove(temp!);

		return temp!.Value;
	}

	private readonly Random ran = new Random();
	private IVariable<int> GetRandomVariable(LinkedList<IVariable<int>> list)
	{
		var index = ran.Next(0, list.Count - 1);
		var node = list.First;
		while (--index >= 0)
			node = node!.Next;
		list.Remove(node!);
		return node!.Value;
	}

	private IVariable<int> GetFirstVariable(LinkedList<IVariable<int>> list)
	{
		var first = list.First;
		list.Remove(first!);
		return first!.Value;
	}

	private IVariable<int> GetLastVariable(LinkedList<IVariable<int>> list)
	{
		var last = list.Last;
		list.Remove(last!);
		return last!.Value;
	}

	private double ComputeProgress()
	{
		var progress = 0.0;
		var scale = 1.0;

		for (var d = 0; d <= this.Depth && d < this.Variables.Count; ++d)
		{
			if (this.BranchFactor[d] == 0)
				continue;

			progress += scale * this.Explored[d] / this.BranchFactor[d];

			if (this.Explored[d] >= this.BranchFactor[d])
				break;

			scale /= this.BranchFactor[d];
		}

		return progress;
	}
}
