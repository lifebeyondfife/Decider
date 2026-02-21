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
	private double ProgressHighWatermark { get; set; }

	private int[] AssignmentDepthByVarId { get; set; } = Array.Empty<int>();
	private int ConflictJumpDepth { get; set; } = -1;
	private int[] DepthConflictAccumulator { get; set; } = Array.Empty<int>();
	private IConstraint? OptimisationConstraint { get; set; }

	private List<IVariable<int>> AssignmentCandidates { get; set; } = new List<IVariable<int>>();

	public IVariableOrderingHeuristic<int> VariableOrdering { get; private set; }
	public IValueOrderingHeuristic<int> ValueOrdering { get; private set; }

	private IList<int>[] VariableConstraintIndices { get; set; } = Array.Empty<IList<int>>();
	private IList<int> VariableLastSeenGenerations { get; set; } = new List<int>();
	private bool[] InDirtyConstraintSet { get; set; } = Array.Empty<bool>();
	private Queue<int> DirtyConstraintQueue { get; set; } = new Queue<int>();
	private int[] PropagationSnapshotBuffer { get; set; } = Array.Empty<int>();

	public StateInteger(IEnumerable<IVariable<int>> variables, IEnumerable<IConstraint> constraints,
		IVariableOrderingHeuristic<int>? ordering = null, IValueOrderingHeuristic<int>? valueOrdering = null)
	{
		SetVariables(variables);
		this.Depth = 0;
		this.Backtracks = 0;
		this.Runtime = new TimeSpan(0);
		this.Solutions = new List<IDictionary<string, IVariable<int>>>();

		this.ProgressInterval = TimeSpan.FromSeconds(1);
		this.LastProgressReport = TimeSpan.Zero;

		this.Trail = new DomainTrail(this.Variables.Count, this.Variables.Count * 10000);

		this.BranchFactor = new int[this.Variables.Count];
		this.Explored = new int[this.Variables.Count];

		this.VariableOrdering = ordering ?? new MostConstrainedOrdering();
		this.ValueOrdering = valueOrdering ?? new LowestValueOrdering();

		for (var i = 0; i < this.Variables.Count; ++i)
			((VariableInteger)this.Variables[i]).SetVariableId(i);

		SetConstraints(constraints);
	}

	public void SetVariables(IEnumerable<IVariable<int>> variableList)
	{
		this.Variables = variableList.ToList();

		foreach (var variable in this.Variables)
			variable.SetState(this);

		this.VariableLastSeenGenerations = new int[this.Variables.Count];
		for (var i = 0; i < this.VariableLastSeenGenerations.Count; ++i)
			this.VariableLastSeenGenerations[i] = -1;

		this.AssignmentDepthByVarId = new int[this.Variables.Count];
		for (var i = 0; i < this.AssignmentDepthByVarId.Length; ++i)
			this.AssignmentDepthByVarId[i] = -1;

		this.DepthConflictAccumulator = new int[this.Variables.Count];
		for (var i = 0; i < this.DepthConflictAccumulator.Length; ++i)
			this.DepthConflictAccumulator[i] = -1;
	}

	public void SetConstraints(IEnumerable<IConstraint> constraints)
	{
		this.Constraints = constraints?.ToList() ?? new List<IConstraint>();
		this.BacktrackableConstraints = this.Constraints.OfType<IBacktrackableConstraint>().ToList();
		BuildVariableConstraintIndex();
	}

	private void BuildVariableConstraintIndex()
	{
		this.VariableConstraintIndices = new IList<int>[this.Variables.Count];
		for (var i = 0; i < this.VariableConstraintIndices.Length; ++i)
			this.VariableConstraintIndices[i] = new List<int>();

		var maxConstraintVariables = 0;
		for (var ci = 0; ci < this.Constraints.Count; ++ci)
		{
			if (this.Constraints[ci] is not IConstraint<int> constraintInt)
				continue;

			foreach (var variable in constraintInt.Variables)
				this.VariableConstraintIndices[variable.VariableId].Add(ci);

			var count = constraintInt.Variables.Count;
			if (count > maxConstraintVariables)
				maxConstraintVariables = count;
		}

		this.VariableLastSeenGenerations = new int[this.Variables.Count];
		for (var i = 0; i < this.VariableLastSeenGenerations.Count; ++i)
			this.VariableLastSeenGenerations[i] = -1;

		this.InDirtyConstraintSet = new bool[this.Constraints.Count];
		this.DirtyConstraintQueue = new Queue<int>(this.Constraints.Count);
		this.PropagationSnapshotBuffer = new int[maxConstraintVariables];
	}

	public void SetVariableOrderingHeuristic(IVariableOrderingHeuristic<int> variableOrdering)
	{
		this.VariableOrdering = variableOrdering;
	}

	public void SetValueOrderingHeuristic(IValueOrderingHeuristic<int> valueOrdering)
	{
		this.ValueOrdering = valueOrdering;
	}

	public StateOperationResult Search()
	{
		this.ProgressHighWatermark = 0;
		this.AssignmentCandidates = this.LastSolution == null
			? new List<IVariable<int>>(this.Variables)
			: new List<IVariable<int>>();
		var instantiatedVariables = this.LastSolution ?? new IVariable<int>[this.Variables.Count];
		var stopwatch = Stopwatch.StartNew();
		var searchResult = StateOperationResult.Unsatisfiable;

		if (this.Depth == instantiatedVariables.Length)
		{
			--this.Depth;
			Backtrack(instantiatedVariables);
			++this.Depth;
		}
		else if (ConstraintsViolated())
		{
			this.Runtime += stopwatch.Elapsed;
			stopwatch.Stop();
			return searchResult;
		}

		if (Search(out searchResult, instantiatedVariables, ref stopwatch))
			this.Solutions.Add(CloneLastSolution());

		this.Runtime += stopwatch.Elapsed;
		stopwatch.Stop();
		return searchResult;
	}

	public StateOperationResult Search(IVariable<int> optimiseVar, CancellationToken cancellationToken = default)
	{
		this.ProgressHighWatermark = 0;
		var instantiatedVariables = new IVariable<int>[this.Variables.Count];
		var stopwatch = Stopwatch.StartNew();
		var searchResult = StateOperationResult.Unsatisfiable;

		var guidedOrdering = new SolutionGuidedValueOrdering(this.ValueOrdering);
		this.ValueOrdering = guidedOrdering;

		this.OptimisationConstraint = new ConstraintInteger((VariableInteger) optimiseVar < Int32.MaxValue);
		this.Constraints.Add(this.OptimisationConstraint);
		BuildVariableConstraintIndex();

		this.Depth = 0;
		this.AssignmentCandidates = new List<IVariable<int>>(this.Variables);

		if (ConstraintsViolated() || this.AssignmentCandidates.Any(v => v.Size() == 0))
		{
			this.Runtime += stopwatch.Elapsed;
			stopwatch.Stop();
			return searchResult;
		}

		while (true)
		{
			if (Search(out searchResult, instantiatedVariables, ref stopwatch, cancellationToken))
			{
				var bestValue = optimiseVar.InstantiatedValue;
				this.OptimalSolution = CloneLastSolution();
				guidedOrdering.UpdatePreferredValues(this.LastSolution!);

				foreach (var variable in this.Variables)
					variable.Backtrack(0);

				this.Depth = -1;
				this.Trail.Backtrack(this.Depth, this.Variables);

				foreach (var constraint in this.BacktrackableConstraints)
					constraint.OnBacktrack(this.Depth);

				this.Constraints.Remove(this.OptimisationConstraint);
				this.OptimisationConstraint = new ConstraintInteger((VariableInteger) optimiseVar < bestValue);
				this.Constraints.Add(this.OptimisationConstraint);
				BuildVariableConstraintIndex();

				this.Depth = 0;
				this.AssignmentCandidates = new List<IVariable<int>>(this.Variables);
				for (var i = 0; i < this.Variables.Count; ++i)
				{
					this.BranchFactor[i] = 0;
					this.Explored[i] = 0;
					this.AssignmentDepthByVarId[i] = -1;
					this.DepthConflictAccumulator[i] = -1;
				}

				this.ProgressHighWatermark = 0;

				if (ConstraintsViolated() || this.AssignmentCandidates.Any(v => v.Size() == 0))
					break;
			}
			else if (searchResult == StateOperationResult.Cancelled)
				break;
			else
				break;
		}

		if (this.OptimalSolution != null && searchResult == StateOperationResult.Unsatisfiable)
			searchResult = StateOperationResult.Solved;

		this.Runtime += stopwatch.Elapsed;
		stopwatch.Stop();
		return searchResult;
	}

	public StateOperationResult SearchAllSolutions()
	{
		this.ProgressHighWatermark = 0;
		this.AssignmentCandidates = this.LastSolution == null
			? new List<IVariable<int>>(this.Variables)
			: new List<IVariable<int>>();
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
				Backtrack(instantiatedVariables);
				++this.Depth;
			}
			else if (ConstraintsViolated())
			{
				this.Runtime += stopwatch.Elapsed;
				stopwatch.Stop();
				break;
			}

			if (Search(out searchResult, instantiatedVariables, ref stopwatch))
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

	private bool Search(out StateOperationResult searchResult,
		IList<IVariable<int>> instantiatedVariables, ref Stopwatch stopwatch, CancellationToken cancellationToken = default)
	{
		searchResult = StateOperationResult.Unsatisfiable;
		if (this.AssignmentCandidates.Any(v => v.Size() == 0))
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

			var selectedIndex = this.VariableOrdering.SelectVariableIndex(this.AssignmentCandidates);

			if (this.BranchFactor[this.Depth] != 0 &&
				instantiatedVariables[this.Depth] != null &&
				instantiatedVariables[this.Depth].VariableId != this.AssignmentCandidates[selectedIndex].VariableId)
				this.BranchFactor[this.Depth] = this.Explored[this.Depth] + this.AssignmentCandidates[selectedIndex].Size();

			instantiatedVariables[this.Depth] = this.AssignmentCandidates[selectedIndex];
			var lastIndex = this.AssignmentCandidates.Count - 1;
			this.AssignmentCandidates[selectedIndex] = this.AssignmentCandidates[lastIndex];
			this.AssignmentCandidates.RemoveAt(lastIndex);

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

			var selectedValue = this.ValueOrdering.SelectValue(instantiatedVariables[this.Depth]);
			instantiatedVariables[this.Depth].Instantiate(selectedValue, this.Depth, out DomainOperationResult instantiateResult);

			if (instantiateResult != DomainOperationResult.InstantiateSuccessful)
				return false;

			this.AssignmentDepthByVarId[instantiatedVariables[this.Depth].VariableId] = this.Depth;

			if (ConstraintsViolated() || this.AssignmentCandidates.Any(v => v.Size() == 0))
			{
				if (!Backtrack(instantiatedVariables))
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

	private bool Backtrack(IList<IVariable<int>> instantiatedVariables)
	{
		if (this.ConflictJumpDepth >= 0 && this.ConflictJumpDepth > this.DepthConflictAccumulator[this.Depth])
			this.DepthConflictAccumulator[this.Depth] = this.ConflictJumpDepth;
		this.ConflictJumpDepth = -1;

		return ChronologicalBacktrack(instantiatedVariables);
	}

	private bool ChronologicalBacktrack(IList<IVariable<int>> instantiatedVariables)
	{
		while (true)
		{
			if (this.Depth < 0)
				return false;

			this.AssignmentCandidates.Add(instantiatedVariables[this.Depth]);
			BackTrackVariable(instantiatedVariables[this.Depth], out DomainOperationResult removeResult);

			if (removeResult != DomainOperationResult.EmptyDomain)
			{
				this.DepthConflictAccumulator[this.Depth + 1] = -1;
				return true;
			}

			this.BranchFactor[this.Depth + 1] = 0;

			var jumpTarget = this.DepthConflictAccumulator[this.Depth + 1];
			this.DepthConflictAccumulator[this.Depth + 1] = -1;

			if (this.Depth < 0)
				return false;

			if (jumpTarget >= 0 && jumpTarget > this.DepthConflictAccumulator[this.Depth])
				this.DepthConflictAccumulator[this.Depth] = jumpTarget;

			if (jumpTarget >= 0 && jumpTarget < this.Depth)
				return BacktrackJumpFromCurrentDepth(instantiatedVariables, jumpTarget);
		}
	}

	private bool BacktrackJumpFromCurrentDepth(IList<IVariable<int>> instantiatedVariables, int jumpTarget)
	{
		var currentDepth = this.Depth;

		for (var d = currentDepth; d > jumpTarget; --d)
		{
			this.AssignmentCandidates.Add(instantiatedVariables[d]);
			this.AssignmentDepthByVarId[instantiatedVariables[d].VariableId] = -1;
			this.BranchFactor[d] = 0;
			this.DepthConflictAccumulator[d] = -1;
		}
		this.AssignmentCandidates.Add(instantiatedVariables[jumpTarget]);
		this.AssignmentDepthByVarId[instantiatedVariables[jumpTarget].VariableId] = -1;
		this.DepthConflictAccumulator[jumpTarget] = -1;

		var targetValue = instantiatedVariables[jumpTarget].InstantiatedValue;

		foreach (var variable in this.Variables)
			variable.Backtrack(jumpTarget);

		++this.Explored[jumpTarget];
		this.Depth = jumpTarget - 1;

		this.Trail.Backtrack(this.Depth, this.Variables);

		foreach (var constraint in this.BacktrackableConstraints)
			constraint.OnBacktrack(this.Depth);

		instantiatedVariables[jumpTarget].Remove(targetValue, this.Depth, out DomainOperationResult removeResult);

		if (removeResult != DomainOperationResult.EmptyDomain)
			return true;

		this.BranchFactor[jumpTarget] = 0;
		return ChronologicalBacktrack(instantiatedVariables);
	}

	private int ComputeConflictDepth(IReadOnlyList<IVariable<int>>? constraintVariables)
	{
		if (constraintVariables == null)
			return -1;

		var maxDepth = -1;
		foreach (var variable in constraintVariables)
		{
			var variableId = variable.VariableId;
			if (variableId < 0 || variableId >= this.AssignmentDepthByVarId.Length)
				continue;
			var assignmentDepth = this.AssignmentDepthByVarId[variableId];
			if (assignmentDepth >= 0 && assignmentDepth < this.Depth && assignmentDepth > maxDepth)
				maxDepth = assignmentDepth;
		}
		return maxDepth;
	}

	private bool ConstraintsViolated()
	{
		this.ConflictJumpDepth = -1;
		BuildInitialDirtySet();

		if (ProcessDirtyQueue())
		{
			ClearDirtySet();
			return true;
		}

		SyncVariableGenerations();
		return false;
	}

	private void BuildInitialDirtySet()
	{
		for (var varIndex = 0; varIndex < this.Variables.Count; ++varIndex)
		{
			if (this.Variables[varIndex].Generation == this.VariableLastSeenGenerations[varIndex])
				continue;

			foreach (var conIndex in this.VariableConstraintIndices[varIndex])
			{
				if (this.InDirtyConstraintSet[conIndex])
					continue;

				this.InDirtyConstraintSet[conIndex] = true;
				this.DirtyConstraintQueue.Enqueue(conIndex);
			}
		}
	}

	private bool ProcessDirtyQueue()
	{
		while (this.DirtyConstraintQueue.Count > 0)
		{
			var conIndex = this.DirtyConstraintQueue.Dequeue();
			this.InDirtyConstraintSet[conIndex] = false;

			if (PropagateConstraint(conIndex))
				return true;
		}

		return false;
	}

	private bool PropagateConstraint(int conIndex)
	{
		var constraint = this.Constraints[conIndex];
		var constraintVariables = (constraint as IConstraint<int>)?.Variables;

		SnapshotGenerations(constraintVariables);

		constraint.Propagate(out ConstraintOperationResult propagateResult);
		if ((propagateResult & ConstraintOperationResult.Violated) == ConstraintOperationResult.Violated)
		{
			constraint.FailureWeight++;
			this.ConflictJumpDepth = ComputeConflictDepth(constraintVariables);
			return true;
		}

		EnqueueChangedConstraints(constraintVariables);

		if (!AllInstantiated(constraintVariables))
			return false;

		constraint.Check(out ConstraintOperationResult checkResult);
		if ((checkResult & ConstraintOperationResult.Violated) == ConstraintOperationResult.Violated)
		{
			constraint.FailureWeight++;
			this.ConflictJumpDepth = ComputeConflictDepth(constraintVariables);
			return true;
		}

		return false;
	}

	private void SnapshotGenerations(IReadOnlyList<IVariable<int>>? constraintVariables)
	{
		if (constraintVariables == null)
			return;

		for (var j = 0; j < constraintVariables.Count; ++j)
			this.PropagationSnapshotBuffer[j] = constraintVariables[j].Generation;
	}

	private void EnqueueChangedConstraints(IReadOnlyList<IVariable<int>>? constraintVariables)
	{
		if (constraintVariables == null)
			return;

		for (var j = 0; j < constraintVariables.Count; ++j)
		{
			if (constraintVariables[j].Generation == this.PropagationSnapshotBuffer[j])
				continue;

			foreach (var otherConIndex in this.VariableConstraintIndices[constraintVariables[j].VariableId])
			{
				if (this.InDirtyConstraintSet[otherConIndex])
					continue;

				this.InDirtyConstraintSet[otherConIndex] = true;
				this.DirtyConstraintQueue.Enqueue(otherConIndex);
			}
		}
	}

	private static bool AllInstantiated(IReadOnlyList<IVariable<int>>? constraintVariables)
	{
		if (constraintVariables == null)
			return true;

		foreach (var variable in constraintVariables)
		{
			if (!variable.Instantiated())
				return false;
		}

		return true;
	}

	private void SyncVariableGenerations()
	{
		for (var varIndex = 0; varIndex < this.Variables.Count; ++varIndex)
			this.VariableLastSeenGenerations[varIndex] = this.Variables[varIndex].Generation;
	}

	private void ClearDirtySet()
	{
		while (this.DirtyConstraintQueue.Count > 0)
			this.InDirtyConstraintSet[this.DirtyConstraintQueue.Dequeue()] = false;
	}

	private void BackTrackVariable(IVariable<int> variablePrune, out DomainOperationResult result)
	{
		++this.Backtracks;
		this.AssignmentDepthByVarId[variablePrune.VariableId] = -1;
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

		if (progress > this.ProgressHighWatermark)
			this.ProgressHighWatermark = progress;
		else
			progress = this.ProgressHighWatermark;

		return progress;
	}
}
