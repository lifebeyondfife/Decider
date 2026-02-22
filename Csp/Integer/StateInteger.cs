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

	private PropagationTrail PropTrail { get; set; }
	private ClauseStore LearnedClauses { get; set; }
	public bool ClauseLearningEnabled { get; set; }
	private int[] BoundSnapshotLB { get; set; } = Array.Empty<int>();
	private int[] BoundSnapshotUB { get; set; } = Array.Empty<int>();
	private int ConflictConstraintIndex { get; set; } = -1;

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
		this.PropTrail = new PropagationTrail(this.Variables.Count, this.Variables.Count * 100);
		this.LearnedClauses = new ClauseStore();

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
		this.BoundSnapshotLB = new int[maxConstraintVariables];
		this.BoundSnapshotUB = new int[maxConstraintVariables];
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

				if (this.ClauseLearningEnabled)
				{
					this.LearnedClauses.Clear();
					this.PropTrail.Clear();
				}

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

			var decisionConflict = false;
			if (this.ClauseLearningEnabled)
			{
				this.ConflictConstraintIndex = -1;
				var instVar = instantiatedVariables[this.Depth];
				this.PropTrail.RecordDecision(instVar.VariableId,
					instVar.Domain.LowerBound, instVar.Domain.UpperBound, this.Depth);

				decisionConflict = NotifyClauseStore(instVar.VariableId, false);
				if (!decisionConflict)
					decisionConflict = NotifyClauseStore(instVar.VariableId, true);
			}

			if (decisionConflict || ConstraintsViolated() || this.AssignmentCandidates.Any(v => v.Size() == 0))
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
		if (this.ClauseLearningEnabled && this.ConflictConstraintIndex >= 0)
			return PerformConflictAnalysis(instantiatedVariables);

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

		if (this.ClauseLearningEnabled)
			this.PropTrail.Backtrack(this.Depth);

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
		this.ConflictConstraintIndex = -1;
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

		if (this.ClauseLearningEnabled)
			SnapshotBounds(constraintVariables);

		constraint.Propagate(out ConstraintOperationResult propagateResult);

		if ((propagateResult & ConstraintOperationResult.Violated) == ConstraintOperationResult.Violated)
		{
			constraint.FailureWeight++;
			this.ConflictConstraintIndex = conIndex;
			this.ConflictJumpDepth = ComputeConflictDepth(constraintVariables);

			if (this.ClauseLearningEnabled)
				RecordBoundChangesTrailOnly(constraintVariables, conIndex);

			return true;
		}

		EnqueueChangedConstraints(constraintVariables);

		if (this.ClauseLearningEnabled && RecordBoundChanges(constraintVariables, conIndex))
			return true;

		if (!AllInstantiated(constraintVariables))
			return false;

		constraint.Check(out ConstraintOperationResult checkResult);
		if ((checkResult & ConstraintOperationResult.Violated) == ConstraintOperationResult.Violated)
		{
			constraint.FailureWeight++;
			this.ConflictConstraintIndex = conIndex;
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

	private void SnapshotBounds(IReadOnlyList<IVariable<int>>? constraintVariables)
	{
		if (constraintVariables == null)
			return;

		for (var j = 0; j < constraintVariables.Count; ++j)
		{
			this.BoundSnapshotLB[j] = constraintVariables[j].Domain.LowerBound;
			this.BoundSnapshotUB[j] = constraintVariables[j].Domain.UpperBound;
		}
	}

	private static bool DomainHasHoles(IReadOnlyList<IVariable<int>> constraintVariables)
	{
		foreach (var variable in constraintVariables)
		{
			var domain = variable.Domain;
			var lb = domain.LowerBound;
			var ub = domain.UpperBound;
			for (var v = lb + 1; v < ub; ++v)
			{
				if (!domain.Contains(v))
					return true;
			}
		}

		return false;
	}

	private bool RecordBoundChanges(IReadOnlyList<IVariable<int>>? constraintVariables, int conIndex)
	{
		if (constraintVariables == null)
			return false;

		var constraint = this.Constraints[conIndex];
		var hasExplainer = constraint is IExplainableConstraint;
		var snapshotHadHoles = false;
		if (!hasExplainer)
			snapshotHadHoles = DomainHasHoles(constraintVariables);

		for (var j = 0; j < constraintVariables.Count; ++j)
		{
			if (constraintVariables[j].Generation == this.PropagationSnapshotBuffer[j])
				continue;

			var variable = constraintVariables[j];
			var varId = variable.VariableId;
			var newLB = variable.Domain.LowerBound;
			var newUB = variable.Domain.UpperBound;

			if (newLB > this.BoundSnapshotLB[j])
			{
				var explanation = ComputeEagerExplanation(constraintVariables, conIndex, varId, true);
				this.PropTrail.RecordPropagation(varId, true, newLB, this.Depth,
					PropagationTrail.ReasonConstraint, conIndex, explanation, snapshotHadHoles);

				if (NotifyClauseStore(varId, false))
					return true;
			}

			if (newUB < this.BoundSnapshotUB[j])
			{
				var explanation = ComputeEagerExplanation(constraintVariables, conIndex, varId, false);
				this.PropTrail.RecordPropagation(varId, false, newUB, this.Depth,
					PropagationTrail.ReasonConstraint, conIndex, explanation, snapshotHadHoles);

				if (NotifyClauseStore(varId, true))
					return true;
			}
		}

		return false;
	}

	private IList<BoundReason> ComputeEagerExplanation(IReadOnlyList<IVariable<int>> constraintVariables,
		int conIndex, int propagatedVariableId, bool propagatedIsLowerBound)
	{
		var constraint = this.Constraints[conIndex];
		if (constraint is IExplainableConstraint explainable)
		{
			var result = new List<BoundReason>();
			var variable = this.Variables[propagatedVariableId];
			explainable.Explain(propagatedVariableId, propagatedIsLowerBound,
				propagatedIsLowerBound ? variable.Domain.LowerBound : variable.Domain.UpperBound, result);
			return result;
		}

		var explanation = new List<BoundReason>();
		for (var k = 0; k < constraintVariables.Count; ++k)
		{
			var v = constraintVariables[k];
			if (v.VariableId == propagatedVariableId && propagatedIsLowerBound)
				explanation.Add(new BoundReason(v.VariableId, false, this.BoundSnapshotUB[k]));
			else if (v.VariableId == propagatedVariableId && !propagatedIsLowerBound)
				explanation.Add(new BoundReason(v.VariableId, true, this.BoundSnapshotLB[k]));
			else
			{
				explanation.Add(new BoundReason(v.VariableId, true, this.BoundSnapshotLB[k]));
				explanation.Add(new BoundReason(v.VariableId, false, this.BoundSnapshotUB[k]));
			}
		}

		return explanation;
	}

	private bool NotifyClauseStore(int varId, bool isLowerBound)
	{
		while (true)
		{
			if (this.LearnedClauses.NotifyBoundChange(varId, isLowerBound, this.Variables,
				out var forcedLiteral, out var forcedClauseIndex))
				return true;

			if (forcedClauseIndex < 0)
				return false;

			if (ApplyForcedLiteral(forcedLiteral, forcedClauseIndex))
				return true;
		}
	}

	private bool ApplyForcedLiteral(BoundReason literal, int clauseIndex)
	{
		var variable = this.Variables[literal.VariableIndex];

		if (literal.IsLowerBound)
		{
			while (variable.Domain.LowerBound < literal.BoundValue)
			{
				variable.Remove(variable.Domain.LowerBound, this.Depth, out var result);
				if (result == DomainOperationResult.EmptyDomain)
					return true;
			}
		}
		else
		{
			while (variable.Domain.UpperBound > literal.BoundValue)
			{
				variable.Remove(variable.Domain.UpperBound, this.Depth, out var result);
				if (result == DomainOperationResult.EmptyDomain)
					return true;
			}
		}

		this.PropTrail.RecordPropagation(literal.VariableIndex, literal.IsLowerBound, literal.BoundValue,
			this.Depth, PropagationTrail.ReasonLearnedClause, clauseIndex);

		foreach (var conIndex in this.VariableConstraintIndices[literal.VariableIndex])
		{
			if (this.InDirtyConstraintSet[conIndex])
				continue;

			this.InDirtyConstraintSet[conIndex] = true;
			this.DirtyConstraintQueue.Enqueue(conIndex);
		}

		if (literal.IsLowerBound)
			return NotifyClauseStore(literal.VariableIndex, false);

		return NotifyClauseStore(literal.VariableIndex, true);
	}

	private void RecordBoundChangesTrailOnly(IReadOnlyList<IVariable<int>>? constraintVariables, int conIndex)
	{
		if (constraintVariables == null)
			return;

		var constraint = this.Constraints[conIndex];
		var hasExplainer = constraint is IExplainableConstraint;
		var snapshotHadHoles = false;
		if (!hasExplainer)
			snapshotHadHoles = DomainHasHoles(constraintVariables);

		for (var j = 0; j < constraintVariables.Count; ++j)
		{
			var variable = constraintVariables[j];
			if (variable.Size() == 0)
				continue;

			var varId = variable.VariableId;
			var newLB = variable.Domain.LowerBound;
			var newUB = variable.Domain.UpperBound;

			if (newLB > this.BoundSnapshotLB[j])
			{
				var explanation = ComputeEagerExplanation(constraintVariables, conIndex, varId, true);
				this.PropTrail.RecordPropagation(varId, true, newLB, this.Depth,
					PropagationTrail.ReasonConstraint, conIndex, explanation, snapshotHadHoles);
			}

			if (newUB < this.BoundSnapshotUB[j])
			{
				var explanation = ComputeEagerExplanation(constraintVariables, conIndex, varId, false);
				this.PropTrail.RecordPropagation(varId, false, newUB, this.Depth,
					PropagationTrail.ReasonConstraint, conIndex, explanation, snapshotHadHoles);
			}
		}
	}

	private IList<BoundReason> GetConflictExplanation()
	{
		var result = new List<BoundReason>();

		if (this.ConflictConstraintIndex < 0 || this.ConflictConstraintIndex >= this.Constraints.Count)
			return result;

		var constraint = this.Constraints[this.ConflictConstraintIndex];
		if (constraint is not IConstraint<int> typedConstraint)
			return result;

		for (var j = 0; j < typedConstraint.Variables.Count; ++j)
		{
			var variable = typedConstraint.Variables[j];
			if (variable.Size() == 0)
			{
				result.Add(new BoundReason(variable.VariableId, true, this.BoundSnapshotLB[j]));
				result.Add(new BoundReason(variable.VariableId, false, this.BoundSnapshotUB[j]));
				continue;
			}

			result.Add(new BoundReason(variable.VariableId, true, variable.Domain.LowerBound));
			result.Add(new BoundReason(variable.VariableId, false, variable.Domain.UpperBound));
		}

		return result;
	}

	private bool PerformConflictAnalysis(IList<IVariable<int>> instantiatedVariables)
	{
		var conflictExplanation = GetConflictExplanation();

		if (ConflictAnalyser.Analyse(this.PropTrail, conflictExplanation, this.Depth,
			this.Constraints, this.Variables, out var learnedLiterals, out _))
		{
			this.LearnedClauses.AddClause(learnedLiterals, this.Variables);
			this.LearnedClauses.DecayAllActivities();
			this.LearnedClauses.ReduceDatabase();
		}

		this.ConflictConstraintIndex = -1;
		return ChronologicalBacktrack(instantiatedVariables);
	}

	private bool BackjumpToLevel(IList<IVariable<int>> instantiatedVariables, int assertionLevel,
		BoundReason[] learnedLiterals, int clauseIndex)
	{
		for (var d = this.Depth; d > assertionLevel; --d)
		{
			if (instantiatedVariables[d] == null)
				continue;

			this.AssignmentCandidates.Add(instantiatedVariables[d]);
			this.AssignmentDepthByVarId[instantiatedVariables[d].VariableId] = -1;
			this.BranchFactor[d] = 0;
			this.DepthConflictAccumulator[d] = -1;
		}

		foreach (var variable in this.Variables)
			variable.Backtrack(assertionLevel + 1);

		this.Trail.Backtrack(assertionLevel, this.Variables);
		this.PropTrail.Backtrack(assertionLevel);

		foreach (var constraint in this.BacktrackableConstraints)
			constraint.OnBacktrack(assertionLevel);

		this.Depth = assertionLevel;
		++this.Backtracks;

		var clauseAlreadySatisfied = false;
		BoundReason? assertingLiteral = null;
		foreach (var literal in learnedLiterals)
		{
			var litVar = this.Variables[literal.VariableIndex];
			if (Clause.IsLiteralFalsified(literal, litVar))
				continue;

			if (Clause.IsLiteralSatisfied(literal, litVar))
			{
				clauseAlreadySatisfied = true;
				continue;
			}

			if (assertingLiteral == null)
				assertingLiteral = literal;
		}

		if (clauseAlreadySatisfied)
			return true;

		if (assertingLiteral == null)
			return ChronologicalBacktrack(instantiatedVariables);

		var lit = assertingLiteral.Value;
		var assertingVar = this.Variables[lit.VariableIndex];

		if (lit.IsLowerBound)
		{
			while (assertingVar.Domain.LowerBound < lit.BoundValue)
			{
				assertingVar.Remove(assertingVar.Domain.LowerBound, this.Depth, out var result);
				if (result == DomainOperationResult.EmptyDomain)
					return ChronologicalBacktrack(instantiatedVariables);
			}
		}
		else
		{
			while (assertingVar.Domain.UpperBound > lit.BoundValue)
			{
				assertingVar.Remove(assertingVar.Domain.UpperBound, this.Depth, out var result);
				if (result == DomainOperationResult.EmptyDomain)
					return ChronologicalBacktrack(instantiatedVariables);
			}
		}

		this.PropTrail.RecordPropagation(lit.VariableIndex, lit.IsLowerBound, lit.BoundValue,
			this.Depth, PropagationTrail.ReasonLearnedClause, clauseIndex);

		return true;
	}

	private void BackTrackVariable(IVariable<int> variablePrune, out DomainOperationResult result)
	{
		++this.Backtracks;
		this.AssignmentDepthByVarId[variablePrune.VariableId] = -1;

		if (!variablePrune.Instantiated())
		{
			foreach (var variable in this.Variables)
				variable.Backtrack(this.Depth);

			--this.Depth;

			this.Trail.Backtrack(this.Depth, this.Variables);

			if (this.ClauseLearningEnabled)
				this.PropTrail.Backtrack(this.Depth);

			foreach (var backtrackableConstraint in this.BacktrackableConstraints)
				backtrackableConstraint.OnBacktrack(this.Depth);

			result = DomainOperationResult.EmptyDomain;
			return;
		}

		var value = variablePrune.InstantiatedValue;

		foreach (var variable in this.Variables)
			variable.Backtrack(this.Depth);

		--this.Depth;
		++this.Explored[this.Depth + 1];

		this.Trail.Backtrack(this.Depth, this.Variables);

		if (this.ClauseLearningEnabled)
			this.PropTrail.Backtrack(this.Depth);

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
