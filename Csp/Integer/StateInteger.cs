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
	public int LearnedClauseCount => this.LearnedClauses.Count;
	public int ClausesLearned => this.LearnedClauses.ClausesLearned;
	public double AverageClauseSize => this.LearnedClauses.AverageClauseSize;
	public int MaxClauseSize => this.LearnedClauses.MaxClauseSize;
	public int UnitPropagationsFromClauses => this.LearnedClauses.UnitPropagationsFromClauses;
	public int ClauseCacheHits => this.LearnedClauses.ClauseCacheHits;
	public int ClausesEvicted => this.LearnedClauses.ClausesEvicted;
	public bool ClauseLearningEnabled { get; set; }
	public bool BackjumpingEnabled { get; set; } = true;
	private bool EnumeratingAllSolutions { get; set; }
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

	public StateOperationResult Search(CancellationToken cancellationToken = default)
	{
		foreach (var v in this.Variables)
			((VariableInteger) v).BoundsOnlyRemove = this.ClauseLearningEnabled;
		this.EnumeratingAllSolutions = false;
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

		if (Search(out searchResult, instantiatedVariables, ref stopwatch, cancellationToken))
			this.Solutions.Add(CloneLastSolution());

		this.Runtime += stopwatch.Elapsed;
		stopwatch.Stop();
		return searchResult;
	}

	public StateOperationResult Search(IVariable<int> optimiseVar, CancellationToken cancellationToken = default)
	{
		foreach (var v in this.Variables)
			((VariableInteger) v).BoundsOnlyRemove = this.ClauseLearningEnabled;
		this.EnumeratingAllSolutions = false;
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
					this.PropTrail.Clear();

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

	public StateOperationResult SearchAllSolutions(CancellationToken cancellationToken = default)
	{
		foreach (var v in this.Variables)
			((VariableInteger) v).BoundsOnlyRemove = this.ClauseLearningEnabled;
		this.EnumeratingAllSolutions = true;
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

			if (Search(out searchResult, instantiatedVariables, ref stopwatch, cancellationToken))
				this.Solutions.Add(CloneLastSolution());

			if (searchResult == StateOperationResult.Cancelled)
			{
				this.Runtime += stopwatch.Elapsed;
				stopwatch.Stop();
				return searchResult;
			}
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

			SelectVariable(instantiatedVariables);

			if (!MakeDecision(instantiatedVariables, out var decisionConflict))
				return false;

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

			ReportProgress(stopwatch);
			++this.Depth;
		}
	}

	private void SelectVariable(IList<IVariable<int>> instantiatedVariables)
	{
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
	}

	private bool MakeDecision(IList<IVariable<int>> instantiatedVariables, out bool decisionConflict)
	{
		decisionConflict = false;
		var variable = instantiatedVariables[this.Depth];
		var selectedValue = this.ValueOrdering.SelectValue(variable);

		if (this.ClauseLearningEnabled)
		{
			var domain = variable.Domain;
			if (selectedValue != domain.LowerBound && selectedValue != domain.UpperBound)
				throw new InvalidOperationException(
					$"Clause learning requires boundary value selection. Value ordering selected {selectedValue} " +
					$"for variable '{variable.Name}' with bounds [{domain.LowerBound}, {domain.UpperBound}]. " +
					$"Use LowestValueOrdering or another boundary-selecting heuristic when clause learning is enabled.");
		}

		variable.Instantiate(selectedValue, this.Depth, out var instantiateResult);

		if (instantiateResult != DomainOperationResult.InstantiateSuccessful)
			return false;

		if (this.ClauseLearningEnabled)
		{
			this.ConflictConstraintIndex = -1;
			this.PropTrail.RecordDecision(variable.VariableId,
				variable.Domain.LowerBound, variable.Domain.UpperBound, this.Depth);

			decisionConflict = NotifyClauseStore(variable.VariableId, false);
			if (!decisionConflict)
				decisionConflict = NotifyClauseStore(variable.VariableId, true);
		}

		return true;
	}

	private void ReportProgress(Stopwatch stopwatch)
	{
		var currentRuntime = this.Runtime + stopwatch.Elapsed;
		if (this.OnProgress == null || currentRuntime - this.LastProgressReport < this.ProgressInterval)
			return;

		this.OnProgress(ComputeProgress());
		this.LastProgressReport = currentRuntime;
	}

	private bool Backtrack(IList<IVariable<int>> instantiatedVariables)
	{
		if (this.ClauseLearningEnabled && this.ConflictConstraintIndex >= 0)
			return PerformConflictAnalysis(instantiatedVariables);

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
				return true;

			this.BranchFactor[this.Depth + 1] = 0;
		}
	}

	private bool ConstraintsViolated()
	{
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

	private bool RecordBoundChanges(IReadOnlyList<IVariable<int>>? constraintVariables, int conIndex)
	{
		if (constraintVariables == null)
			return false;

		List<(int VarId, bool WatchKey)>? notifications = null;
		var snapshotBatch = -1;

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
				if (snapshotBatch < 0)
					snapshotBatch = this.PropTrail.AddSnapshot(this.BoundSnapshotLB, this.BoundSnapshotUB, constraintVariables.Count);

				this.PropTrail.RecordPropagation(varId, true, newLB, this.Depth,
					PropagationTrail.ReasonConstraint, conIndex, snapshotBatch: snapshotBatch);

				(notifications ??= new List<(int, bool)>()).Add((varId, false));
			}

			if (newUB < this.BoundSnapshotUB[j])
			{
				if (snapshotBatch < 0)
					snapshotBatch = this.PropTrail.AddSnapshot(this.BoundSnapshotLB, this.BoundSnapshotUB, constraintVariables.Count);

				this.PropTrail.RecordPropagation(varId, false, newUB, this.Depth,
					PropagationTrail.ReasonConstraint, conIndex, snapshotBatch: snapshotBatch);

				(notifications ??= new List<(int, bool)>()).Add((varId, true));
			}
		}

		if (notifications == null)
			return false;

		foreach (var notification in notifications)
		{
			if (NotifyClauseStore(notification.VarId, notification.WatchKey))
				return true;
		}

		return false;
	}

	private IList<BoundReason> MaterialiseExplanation(int entryIndex)
	{
		var stored = this.PropTrail.GetExplanation(entryIndex);
		if (stored != null)
			return stored;

		ref var entry = ref this.PropTrail.GetEntry(entryIndex);
		if (entry.ReasonKind != PropagationTrail.ReasonConstraint ||
			entry.ReasonIndex < 0 || entry.ReasonIndex >= this.Constraints.Count)
			return new List<BoundReason>();

		var snapshotLower = this.PropTrail.GetSnapshotLower(entry.SnapshotBatch);
		var snapshotUpper = this.PropTrail.GetSnapshotUpper(entry.SnapshotBatch);
		var constraint = this.Constraints[entry.ReasonIndex];
		var result = new List<BoundReason>();

		if (constraint is IExplainableConstraint explainable)
		{
			explainable.Explain(entry.VariableId, entry.IsLowerBound, entry.NewBound,
				snapshotLower, snapshotUpper, result);
			return result;
		}

		var constraintVariables = ((IConstraint<int>) constraint).Variables;
		for (var k = 0; k < constraintVariables.Count; ++k)
		{
			var v = constraintVariables[k];
			if (v.VariableId == entry.VariableId && entry.IsLowerBound)
				result.Add(new BoundReason(v.VariableId, false, snapshotUpper[k]));
			else if (v.VariableId == entry.VariableId && !entry.IsLowerBound)
				result.Add(new BoundReason(v.VariableId, true, snapshotLower[k]));
			else
			{
				result.Add(new BoundReason(v.VariableId, true, snapshotLower[k]));
				result.Add(new BoundReason(v.VariableId, false, snapshotUpper[k]));
			}
		}

		return result;
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

		var explanation = ComputeClauseExplanation(literal, clauseIndex);
		this.PropTrail.RecordPropagation(literal.VariableIndex, literal.IsLowerBound, literal.BoundValue,
			this.Depth, PropagationTrail.ReasonLearnedClause, clauseIndex, explanation);

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

	private IList<BoundReason> ComputeClauseExplanation(BoundReason forcedLiteral, int clauseIndex)
	{
		var clauseLiterals = this.LearnedClauses.GetClauseLiterals(clauseIndex);
		var explanation = new List<BoundReason>();

		foreach (var lit in clauseLiterals)
		{
			if (lit.VariableIndex == forcedLiteral.VariableIndex &&
				lit.IsLowerBound == forcedLiteral.IsLowerBound &&
				lit.BoundValue == forcedLiteral.BoundValue)
				continue;

			explanation.Add(lit.IsLowerBound
				? new BoundReason(lit.VariableIndex, false, lit.BoundValue - 1)
				: new BoundReason(lit.VariableIndex, true, lit.BoundValue + 1));
		}

		return explanation;
	}

	private void RecordBoundChangesTrailOnly(IReadOnlyList<IVariable<int>>? constraintVariables, int conIndex)
	{
		if (constraintVariables == null)
			return;

		var snapshotBatch = -1;

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
				if (snapshotBatch < 0)
					snapshotBatch = this.PropTrail.AddSnapshot(this.BoundSnapshotLB, this.BoundSnapshotUB, constraintVariables.Count);

				this.PropTrail.RecordPropagation(varId, true, newLB, this.Depth,
					PropagationTrail.ReasonConstraint, conIndex, snapshotBatch: snapshotBatch);
			}

			if (newUB < this.BoundSnapshotUB[j])
			{
				if (snapshotBatch < 0)
					snapshotBatch = this.PropTrail.AddSnapshot(this.BoundSnapshotLB, this.BoundSnapshotUB, constraintVariables.Count);

				this.PropTrail.RecordPropagation(varId, false, newUB, this.Depth,
					PropagationTrail.ReasonConstraint, conIndex, snapshotBatch: snapshotBatch);
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
		this.ConflictConstraintIndex = -1;

		if (!ConflictAnalyser.Analyse(this.PropTrail, conflictExplanation, this.Depth,
			MaterialiseExplanation, out var learnedLiterals, out var assertionLevel, out var isAsserting))
			return ChronologicalBacktrack(instantiatedVariables);

		var clauseIndex = this.LearnedClauses.AddClause(learnedLiterals, this.Variables);
		this.LearnedClauses.DecayAllActivities();
		this.LearnedClauses.ReduceDatabase();

		if (!this.BackjumpingEnabled || this.EnumeratingAllSolutions || !isAsserting || assertionLevel >= this.Depth)
			return ChronologicalBacktrack(instantiatedVariables);

		return BackjumpToLevel(instantiatedVariables, assertionLevel, learnedLiterals, clauseIndex);
	}

	private bool BackjumpToLevel(IList<IVariable<int>> instantiatedVariables, int assertionLevel,
		BoundReason[] learnedLiterals, int clauseIndex)
	{
		for (var d = this.Depth; d > assertionLevel; --d)
		{
			if (instantiatedVariables[d] == null)
				continue;

			this.AssignmentCandidates.Add(instantiatedVariables[d]);
			this.BranchFactor[d] = 0;
		}

		foreach (var variable in this.Variables)
			variable.Backtrack(assertionLevel + 1);

		this.Trail.Backtrack(assertionLevel, this.Variables);
		this.PropTrail.Backtrack(assertionLevel);

		foreach (var constraint in this.BacktrackableConstraints)
			constraint.OnBacktrack(assertionLevel);

		this.Depth = assertionLevel;
		++this.Backtracks;

		var assertingLiteralIndex = -1;
		for (var i = 0; i < learnedLiterals.Length; ++i)
		{
			var literal = learnedLiterals[i];
			var literalVariable = this.Variables[literal.VariableIndex];

			if (Clause.IsLiteralSatisfied(literal, literalVariable))
				throw new DeciderException(
					"Conflict analysis produced a clause already satisfied at its assertion level; " +
					"the propagation trail is inconsistent. Set BackjumpingEnabled = false to work around.");

			if (Clause.IsLiteralFalsified(literal, literalVariable))
				continue;

			if (assertingLiteralIndex >= 0)
				throw new DeciderException(
					"Conflict analysis produced a non-asserting clause at its assertion level; " +
					"the propagation trail is inconsistent. Set BackjumpingEnabled = false to work around.");

			assertingLiteralIndex = i;
		}

		if (assertingLiteralIndex < 0)
			return ChronologicalBacktrack(instantiatedVariables);

		var lit = learnedLiterals[assertingLiteralIndex];
		var assertingVariable = this.Variables[lit.VariableIndex];

		if (lit.IsLowerBound)
		{
			while (assertingVariable.Domain.LowerBound < lit.BoundValue)
			{
				assertingVariable.Remove(assertingVariable.Domain.LowerBound, this.Depth, out var result);
				if (result == DomainOperationResult.EmptyDomain)
					return ChronologicalBacktrack(instantiatedVariables);
			}
		}
		else
		{
			while (assertingVariable.Domain.UpperBound > lit.BoundValue)
			{
				assertingVariable.Remove(assertingVariable.Domain.UpperBound, this.Depth, out var result);
				if (result == DomainOperationResult.EmptyDomain)
					return ChronologicalBacktrack(instantiatedVariables);
			}
		}

		this.PropTrail.RecordPropagation(lit.VariableIndex, lit.IsLowerBound, lit.BoundValue,
			this.Depth, PropagationTrail.ReasonLearnedClause, clauseIndex,
			ComputeAssertionExplanation(learnedLiterals, assertingLiteralIndex));

		if (NotifyClauseStore(lit.VariableIndex, !lit.IsLowerBound))
			return ChronologicalBacktrack(instantiatedVariables);

		return true;
	}

	private static IList<BoundReason> ComputeAssertionExplanation(BoundReason[] learnedLiterals,
		int assertingLiteralIndex)
	{
		var explanation = new List<BoundReason>();
		for (var i = 0; i < learnedLiterals.Length; ++i)
		{
			if (i == assertingLiteralIndex)
				continue;

			var literal = learnedLiterals[i];
			explanation.Add(literal.IsLowerBound
				? new BoundReason(literal.VariableIndex, false, literal.BoundValue - 1)
				: new BoundReason(literal.VariableIndex, true, literal.BoundValue + 1));
		}

		return explanation;
	}

	private void BackTrackVariable(IVariable<int> variablePrune, out DomainOperationResult result)
	{
		++this.Backtracks;

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

		var oldLowerBound = variablePrune.Domain.LowerBound;
		var oldUpperBound = variablePrune.Domain.UpperBound;

		variablePrune.Remove(value, this.Depth, out result);

		if (!this.ClauseLearningEnabled || result == DomainOperationResult.EmptyDomain || this.Depth < 0)
			return;

		if (variablePrune.Domain.LowerBound > oldLowerBound)
		{
			this.PropTrail.RecordPropagation(variablePrune.VariableId, true, variablePrune.Domain.LowerBound,
				this.Depth, PropagationTrail.ReasonDecision, -1);

			if (NotifyClauseStore(variablePrune.VariableId, false))
			{
				result = DomainOperationResult.EmptyDomain;
				return;
			}
		}

		if (variablePrune.Domain.UpperBound < oldUpperBound)
		{
			this.PropTrail.RecordPropagation(variablePrune.VariableId, false, variablePrune.Domain.UpperBound,
				this.Depth, PropagationTrail.ReasonDecision, -1);

			if (NotifyClauseStore(variablePrune.VariableId, true))
				result = DomainOperationResult.EmptyDomain;
		}
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
