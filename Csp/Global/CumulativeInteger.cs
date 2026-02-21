/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System;
using System.Collections.Generic;
using System.Linq;

using Decider.Csp.BaseTypes;
using Decider.Csp.Integer;

namespace Decider.Csp.Global;

public class CumulativeInteger : IConstraint<int>, IReasoningConstraint
{
	private IList<VariableInteger> Starts { get; set; }

	public IReadOnlyList<IVariable<int>> Variables => (IReadOnlyList<VariableInteger>)this.Starts;
	private IList<int> Durations { get; set; }
	private IList<int> Demands { get; set; }
	private int Capacity { get; set; }
	private int[] GenerationArray { get; set; }

	public int FailureWeight { get; set; }
	public bool GenerateReasons { get; set; }
	public IList<BoundReason>? LastReason { get; private set; }

	private IState<int>? State { get; set; }
	private int Depth
	{
		get
		{
			if (this.State == null)
				this.State = this.Starts[0].State;

			return this.State!.Depth;
		}
	}

	private int[] TimetableEarliestStartTimes { get; set; }
	private int[] TimetableLatestCompletionTimes { get; set; }
	private int[] TimetableFreeDurations { get; set; }
	private long[] TimetableFreeEnergies { get; set; }
	private long[] TimetableProfileEnergyAtEst { get; set; }
	private long[] TimetableProfileEnergyAtLct { get; set; }
	private int[] TimetableNewBounds { get; set; }
	private List<int> TimetableFreeTaskIndices { get; set; }
	private List<int> TimetableSortedTasks { get; set; }

	private int[] NotFirstLastSortedTasks { get; set; }
	private Comparison<int> NotFirstComparison { get; set; }
	private Comparison<int> NotLastComparison { get; set; }

	private IList<DisjunctiveInteger> DisjunctiveSubproblems { get; set; }
	private bool AllDisjunctive { get; set; }

	public CumulativeInteger(IEnumerable<IVariable<int>> starts, IEnumerable<int> durations, IEnumerable<int> demands, int capacity)
	{
		this.Starts = starts.Cast<VariableInteger>().ToList();
		this.Durations = durations.ToList();
		this.Demands = demands.ToList();

		if (this.Starts.Count != this.Durations.Count || this.Starts.Count != this.Demands.Count)
			throw new ArgumentException("starts, durations, and demands must have the same length");

		if (capacity < 0)
			throw new ArgumentException("capacity must be non-negative");

		this.Capacity = capacity;
		this.GenerationArray = new int[this.Starts.Count];

		var n = this.Starts.Count;
		this.TimetableEarliestStartTimes = new int[n];
		this.TimetableLatestCompletionTimes = new int[n];
		this.TimetableFreeDurations = new int[n];
		this.TimetableFreeEnergies = new long[n];
		this.TimetableProfileEnergyAtEst = new long[n];
		this.TimetableProfileEnergyAtLct = new long[n];
		this.TimetableNewBounds = new int[n];
		this.TimetableFreeTaskIndices = new List<int>(n);
		this.TimetableSortedTasks = new List<int>(n);

		this.NotFirstLastSortedTasks = new int[n];
		for (var i = 0; i < n; ++i)
			this.NotFirstLastSortedTasks[i] = i;

		this.NotFirstComparison = (a, b) => (this.Starts[a].Domain.UpperBound + this.Durations[a]).CompareTo(this.Starts[b].Domain.UpperBound + this.Durations[b]);
		this.NotLastComparison = (a, b) => this.Starts[b].Domain.LowerBound.CompareTo(this.Starts[a].Domain.LowerBound);

		this.DisjunctiveSubproblems = BuildDisjunctiveSubproblems();
		this.AllDisjunctive = this.DisjunctiveSubproblems.Count == 1 && this.DisjunctiveSubproblems[0].Variables.Count == n;
	}

	private IList<DisjunctiveInteger> BuildDisjunctiveSubproblems()
	{
		var n = this.Starts.Count;
		var conflicts = new bool[n, n];
		var hasConflict = new bool[n];

		for (var i = 0; i < n; ++i)
		{
			for (var j = i + 1; j < n; ++j)
			{
				if (this.Demands[i] + this.Demands[j] <= this.Capacity)
					continue;

				conflicts[i, j] = true;
				conflicts[j, i] = true;
				hasConflict[i] = true;
				hasConflict[j] = true;
			}
		}

		var conflictingTasks = new List<int>();
		for (var i = 0; i < n; ++i)
		{
			if (hasConflict[i])
				conflictingTasks.Add(i);
		}

		if (conflictingTasks.Count < 2)
			return new List<DisjunctiveInteger>();

		var allMutuallyConflicting = true;
		for (var ci = 0; ci < conflictingTasks.Count && allMutuallyConflicting; ++ci)
		{
			for (var cj = ci + 1; cj < conflictingTasks.Count && allMutuallyConflicting; ++cj)
			{
				if (!conflicts[conflictingTasks[ci], conflictingTasks[cj]])
					allMutuallyConflicting = false;
			}
		}

		if (allMutuallyConflicting)
		{
			var starts = conflictingTasks.Select(i => this.Starts[i]).ToList();
			var durations = conflictingTasks.Select(i => this.Durations[i]).ToList();
			return new List<DisjunctiveInteger> { new DisjunctiveInteger(starts, durations) };
		}

		return BuildCliques(conflicts, conflictingTasks);
	}

	private IList<DisjunctiveInteger> BuildCliques(bool[,] conflicts, List<int> conflictingTasks)
	{
		var cliques = new List<List<int>>();
		var assigned = new bool[this.Starts.Count];

		foreach (var seed in conflictingTasks)
		{
			if (assigned[seed])
				continue;

			var clique = new List<int> { seed };

			foreach (var candidate in conflictingTasks)
			{
				if (candidate == seed || assigned[candidate])
					continue;

				var fitsClique = true;
				foreach (var member in clique)
				{
					if (conflicts[candidate, member])
						continue;

					fitsClique = false;
					break;
				}

				if (fitsClique)
					clique.Add(candidate);
			}

			if (clique.Count >= 2)
			{
				foreach (var task in clique)
					assigned[task] = true;

				cliques.Add(clique);
			}
		}

		var result = new List<DisjunctiveInteger>();
		foreach (var clique in cliques)
		{
			var starts = clique.Select(i => this.Starts[i]).ToList();
			var durations = clique.Select(i => this.Durations[i]).ToList();
			result.Add(new DisjunctiveInteger(starts, durations));
		}

		return result;
	}

	public void Check(out ConstraintOperationResult result)
	{
		for (var i = 0; i < this.Starts.Count; ++i)
			this.GenerationArray[i] = this.Starts[i].Generation;

		for (var i = 0; i < this.Starts.Count; ++i)
		{
			if (!this.Starts[i].Instantiated())
			{
				result = ConstraintOperationResult.Undecided;
				return;
			}
		}

		var profile = new Dictionary<int, int>();
		for (var i = 0; i < this.Starts.Count; ++i)
		{
			var start = this.Starts[i].InstantiatedValue;
			var end = start + this.Durations[i];
			for (var t = start; t < end; ++t)
			{
				if (!profile.ContainsKey(t))
					profile[t] = 0;
				profile[t] += this.Demands[i];
			}
		}

		foreach (var demand in profile.Values)
		{
			if (demand > this.Capacity)
			{
				result = ConstraintOperationResult.Violated;
				return;
			}
		}

		result = ConstraintOperationResult.Satisfied;
	}

	public void Propagate(out ConstraintOperationResult result)
	{
		result = ConstraintOperationResult.Undecided;

		if (this.GenerateReasons)
			this.LastReason = null;

		var propagationOccurred = true;

		while (propagationOccurred)
		{
			propagationOccurred = false;

			foreach (var disjunctive in this.DisjunctiveSubproblems)
			{
				if (!disjunctive.StateChanged())
					continue;

				disjunctive.GenerateReasons = this.GenerateReasons;
				disjunctive.Propagate(out var disjunctiveResult);

				if (this.GenerateReasons && disjunctive.LastReason != null)
					this.LastReason = disjunctive.LastReason;

				if (ApplySubResult(disjunctiveResult, ref result, ref propagationOccurred))
					return;
			}

			if (this.AllDisjunctive)
				continue;

			var profile = BuildCompulsoryProfile();
			BuildProfileEnergy(profile);

			foreach (var (time, cumulativeDemand) in profile)
			{
				if (cumulativeDemand <= this.Capacity)
					continue;

				if (this.GenerateReasons)
					this.LastReason = ReasonForProfileOverload(time);

				result = ConstraintOperationResult.Violated;
				return;
			}

			for (var i = 0; i < this.Starts.Count; ++i)
			{
				if (this.Starts[i].Instantiated())
					continue;

				var newLowerBound = FindNewLowerBound(i, profile);
				var newUpperBound = FindNewUpperBound(i, profile);

				if (newLowerBound > newUpperBound)
				{
					if (this.GenerateReasons)
						this.LastReason = ReasonForTimetableFilter(i, this.Starts[i].Domain.LowerBound, profile);

					result = ConstraintOperationResult.Violated;
					return;
				}

				this.Starts[i].Propagate(new Bounds<int>(newLowerBound, newUpperBound), out var propagateResult);

				if (propagateResult == ConstraintOperationResult.Propagated)
				{
					result = ConstraintOperationResult.Propagated;
					propagationOccurred = true;
				}
			}

			if (ApplySubResult(NotFirstOrLastRule(true), ref result, ref propagationOccurred))
				return;

			if (ApplySubResult(NotFirstOrLastRule(false), ref result, ref propagationOccurred))
				return;

			if (ApplySubResult(TimetableEdgeFinding(true), ref result, ref propagationOccurred))
				return;

			if (ApplySubResult(TimetableEdgeFinding(false), ref result, ref propagationOccurred))
				return;
		}
	}

	private bool GetCompulsoryPart(int taskIndex, out int compulsoryStart, out int compulsoryEnd)
	{
		var earliestStartTime = this.Starts[taskIndex].Domain.LowerBound;
		var lastStartTime = this.Starts[taskIndex].Domain.UpperBound;
		var duration = this.Durations[taskIndex];

		if (lastStartTime >= earliestStartTime + duration)
		{
			compulsoryStart = int.MaxValue;
			compulsoryEnd = int.MinValue;
			return false;
		}

		compulsoryStart = lastStartTime;
		compulsoryEnd = earliestStartTime + duration;
		return true;
	}

	private bool ApplyNewLowerBound(int taskIndex, int newLowerBound, ref ConstraintOperationResult result)
	{
		if (newLowerBound > this.Starts[taskIndex].Domain.UpperBound)
			return true;

		if (newLowerBound <= this.Starts[taskIndex].Domain.LowerBound)
			return false;

		var bounds = new Bounds<int>(newLowerBound, this.Starts[taskIndex].Domain.UpperBound);
		this.Starts[taskIndex].Propagate(bounds, out var propagateResult);

		if (propagateResult == ConstraintOperationResult.Violated)
			return true;

		if (propagateResult == ConstraintOperationResult.Propagated)
			result = ConstraintOperationResult.Propagated;

		return false;
	}

	private bool ApplyNewUpperBound(int taskIndex, int newUpperBound, ref ConstraintOperationResult result)
	{
		if (newUpperBound < this.Starts[taskIndex].Domain.LowerBound)
			return true;

		if (newUpperBound >= this.Starts[taskIndex].Domain.UpperBound)
			return false;

		var bounds = new Bounds<int>(this.Starts[taskIndex].Domain.LowerBound, newUpperBound);
		this.Starts[taskIndex].Propagate(bounds, out var propagateResult);

		if (propagateResult == ConstraintOperationResult.Violated)
			return true;

		if (propagateResult == ConstraintOperationResult.Propagated)
			result = ConstraintOperationResult.Propagated;

		return false;
	}

	private static bool ApplySubResult(ConstraintOperationResult subResult, ref ConstraintOperationResult result, ref bool propagationOccurred)
	{
		if (subResult == ConstraintOperationResult.Violated)
		{
			result = ConstraintOperationResult.Violated;
			return true;
		}

		if (subResult == ConstraintOperationResult.Propagated)
		{
			result = ConstraintOperationResult.Propagated;
			propagationOccurred = true;
		}

		return false;
	}

	private void SetReasonIfEnabled(IList<int>? contributors, int taskIndex)
	{
		if (!this.GenerateReasons || contributors == null)
			return;

		this.LastReason = CollectReasonForTasks(contributors.Concat(new[] { taskIndex }));
	}

	private List<(int Time, int CumulativeDemand)> BuildCompulsoryProfile()
	{
		var events = new List<(int Time, int Delta)>();

		for (var i = 0; i < this.Starts.Count; ++i)
		{
			if (!GetCompulsoryPart(i, out var compulsoryStart, out var compulsoryEnd))
				continue;

			events.Add((compulsoryStart, this.Demands[i]));
			events.Add((compulsoryEnd, -this.Demands[i]));
		}

		if (events.Count == 0)
			return new List<(int, int)>();

		events.Sort((a, b) => a.Time.CompareTo(b.Time));

		var profile = new List<(int Time, int CumulativeDemand)>();
		var runningTotal = 0;

		foreach (var (time, delta) in events)
		{
			runningTotal += delta;

			if (profile.Count > 0 && profile[profile.Count - 1].Time == time)
				profile[profile.Count - 1] = (time, runningTotal);
			else
				profile.Add((time, runningTotal));
		}

		return profile;
	}

	private static int GetProfileDemandAt(List<(int Time, int CumulativeDemand)> profile, int time)
	{
		if (profile.Count == 0 || time < profile[0].Time)
			return 0;

		var lo = 0;
		var hi = profile.Count - 1;

		while (lo < hi)
		{
			var mid = lo + (hi - lo + 1) / 2;

			if (profile[mid].Time <= time)
				lo = mid;
			else
				hi = mid - 1;
		}

		return profile[lo].CumulativeDemand;
	}

	private static int FindFirstProfileIndex(List<(int Time, int CumulativeDemand)> profile, int minTime)
	{
		var lo = 0;
		var hi = profile.Count;

		while (lo < hi)
		{
			var mid = lo + (hi - lo) / 2;

			if (profile[mid].Time < minTime)
				lo = mid + 1;
			else
				hi = mid;
		}

		return lo;
	}

	private int FindFirstViolatingTime(int taskIndex, int candidateStart, List<(int Time, int CumulativeDemand)> profile)
	{
		var end = candidateStart + this.Durations[taskIndex];
		var demand = this.Demands[taskIndex];
		GetCompulsoryPart(taskIndex, out var compulsoryStart, out var compulsoryEnd);

		var profileDemand = GetProfileDemandAt(profile, candidateStart);
		var taskInCompulsory = candidateStart >= compulsoryStart && candidateStart < compulsoryEnd;
		if ((taskInCompulsory ? profileDemand : profileDemand + demand) > this.Capacity)
			return candidateStart;

		var idx = FindFirstProfileIndex(profile, candidateStart + 1);
		for (; idx < profile.Count && profile[idx].Time < end; ++idx)
		{
			taskInCompulsory = profile[idx].Time >= compulsoryStart && profile[idx].Time < compulsoryEnd;
			var totalDemand = taskInCompulsory ? profile[idx].CumulativeDemand : profile[idx].CumulativeDemand + demand;

			if (totalDemand > this.Capacity)
				return profile[idx].Time;
		}

		if (compulsoryEnd > candidateStart && compulsoryEnd < end)
		{
			profileDemand = GetProfileDemandAt(profile, compulsoryEnd);

			if (profileDemand + demand > this.Capacity)
				return compulsoryEnd;
		}

		return -1;
	}

	private int FindNewLowerBound(int taskIndex, List<(int Time, int CumulativeDemand)> profile)
	{
		var candidate = this.Starts[taskIndex].Domain.LowerBound;
		var upperBound = this.Starts[taskIndex].Domain.UpperBound;

		while (candidate <= upperBound)
		{
			var violatingTime = FindFirstViolatingTime(taskIndex, candidate, profile);
			if (violatingTime < 0)
				return candidate;
			candidate = violatingTime + 1;
		}

		return upperBound + 1;
	}

	private int FindNewUpperBound(int taskIndex, List<(int Time, int CumulativeDemand)> profile)
	{
		var candidate = this.Starts[taskIndex].Domain.UpperBound;
		var lowerBound = this.Starts[taskIndex].Domain.LowerBound;

		while (candidate >= lowerBound)
		{
			var violatingTime = FindFirstViolatingTime(taskIndex, candidate, profile);
			if (violatingTime < 0)
				return candidate;
			candidate = violatingTime - this.Durations[taskIndex];
		}

		return lowerBound - 1;
	}

	private IList<BoundReason> ReasonForProfileOverload(int time)
	{
		var reasons = new List<BoundReason>();

		for (var j = 0; j < this.Starts.Count; ++j)
		{
			if (!GetCompulsoryPart(j, out var compulsoryStart, out var compulsoryEnd))
				continue;

			if (time < compulsoryStart || time >= compulsoryEnd)
				continue;

			reasons.Add(new BoundReason(this.Starts[j].VariableId, true, this.Starts[j].Domain.LowerBound));
			reasons.Add(new BoundReason(this.Starts[j].VariableId, false, this.Starts[j].Domain.UpperBound));
		}

		return reasons;
	}

	private IList<BoundReason> ReasonForTimetableFilter(int taskIndex, int candidateStart, List<(int Time, int CumulativeDemand)> profile)
	{
		var violatingTime = FindFirstViolatingTime(taskIndex, candidateStart, profile);

		if (violatingTime < 0)
			return new List<BoundReason>();

		return ReasonForProfileOverload(violatingTime);
	}

	private IList<BoundReason> CollectReasonForTasks(IEnumerable<int> taskIndices)
	{
		var reasons = new List<BoundReason>();

		foreach (var j in taskIndices)
		{
			reasons.Add(new BoundReason(this.Starts[j].VariableId, true, this.Starts[j].Domain.LowerBound));
			reasons.Add(new BoundReason(this.Starts[j].VariableId, false, this.Starts[j].Domain.UpperBound));
		}

		return reasons;
	}

	private List<(int Time, long CumulativeEnergy)> ProfileEnergy { get; set; } = new();

	private void BuildProfileEnergy(List<(int Time, int CumulativeDemand)> profile)
	{
		this.ProfileEnergy.Clear();

		if (profile.Count == 0)
			return;

		this.ProfileEnergy.Add((profile[0].Time, 0L));

		var cumulative = 0L;
		for (var i = 0; i < profile.Count - 1; ++i)
		{
			var segmentLength = profile[i + 1].Time - profile[i].Time;
			cumulative += (long)profile[i].CumulativeDemand * segmentLength;
			this.ProfileEnergy.Add((profile[i + 1].Time, cumulative));
		}
	}

	private long GetProfileEnergy(int time)
	{
		if (this.ProfileEnergy.Count == 0)
			return 0L;

		if (time <= this.ProfileEnergy[0].Time)
			return 0L;

		var last = this.ProfileEnergy[this.ProfileEnergy.Count - 1];
		if (time >= last.Time)
			return last.CumulativeEnergy;

		var lo = 0;
		var hi = this.ProfileEnergy.Count - 1;

		while (lo < hi)
		{
			var mid = lo + (hi - lo + 1) / 2;

			if (this.ProfileEnergy[mid].Time <= time)
				lo = mid;
			else
				hi = mid - 1;
		}

		var (entryTime, entryEnergy) = this.ProfileEnergy[lo];
		var (nextTime, nextEnergy) = this.ProfileEnergy[lo + 1];
		var demandRate = (nextEnergy - entryEnergy) / (nextTime - entryTime);
		return entryEnergy + demandRate * (time - entryTime);
	}

	private static int FloorDiv(long dividend, int divisor)
	{
		var quotient = dividend / divisor;

		if (dividend % divisor != 0 && dividend < 0)
			quotient--;

		return (int)quotient;
	}

	private ConstraintOperationResult TimetableEdgeFinding(bool forward)
	{
		var taskCount = this.Starts.Count;

		if (taskCount < 2 || this.ProfileEnergy.Count == 0)
			return ConstraintOperationResult.Undecided;

		var earliestStartTime = this.TimetableEarliestStartTimes;
		var latestCompletionTime = this.TimetableLatestCompletionTimes;
		var freeDuration = this.TimetableFreeDurations;
		var freeEnergy = this.TimetableFreeEnergies;
		this.TimetableFreeTaskIndices.Clear();

		for (var i = 0; i < taskCount; ++i)
		{
			earliestStartTime[i] = this.Starts[i].Domain.LowerBound;
			latestCompletionTime[i] = this.Starts[i].Domain.UpperBound + this.Durations[i];
			var compulsoryDuration = Math.Max(0, earliestStartTime[i] + this.Durations[i] - latestCompletionTime[i] + this.Durations[i]);
			freeDuration[i] = this.Durations[i] - compulsoryDuration;
			freeEnergy[i] = (long)this.Demands[i] * freeDuration[i];
			this.TimetableProfileEnergyAtEst[i] = GetProfileEnergy(earliestStartTime[i]);
			this.TimetableProfileEnergyAtLct[i] = GetProfileEnergy(latestCompletionTime[i]);

			if (freeDuration[i] > 0 && this.Demands[i] > 0)
				this.TimetableFreeTaskIndices.Add(i);
		}

		if (this.TimetableFreeTaskIndices.Count < 2)
			return ConstraintOperationResult.Undecided;

		return forward
			? TimetableEdgeFindingForward(taskCount)
			: TimetableEdgeFindingBackward(taskCount);
	}

	private ConstraintOperationResult TimetableEdgeFindingForward(int taskCount)
	{
		var result = ConstraintOperationResult.Undecided;
		var earliestStartTime = this.TimetableEarliestStartTimes;
		var latestCompletionTime = this.TimetableLatestCompletionTimes;
		var freeEnergy = this.TimetableFreeEnergies;
		var profileEnergyAtEst = this.TimetableProfileEnergyAtEst;
		var profileEnergyAtLct = this.TimetableProfileEnergyAtLct;
		var newBounds = this.TimetableNewBounds;

		this.TimetableSortedTasks.Clear();
		this.TimetableSortedTasks.AddRange(this.TimetableFreeTaskIndices);
		this.TimetableSortedTasks.Sort((a, b) => earliestStartTime[b].CompareTo(earliestStartTime[a]));

		for (var i = 0; i < taskCount; ++i)
			newBounds[i] = earliestStartTime[i];

		foreach (var outerTask in this.TimetableFreeTaskIndices)
		{
			var setFreeEnergy = 0L;
			var candidateTask = -1;
			var candidateAdditionalEnergy = 0L;

			foreach (var innerTask in this.TimetableSortedTasks)
			{
				if (earliestStartTime[innerTask] >= latestCompletionTime[outerTask])
					continue;

				if (latestCompletionTime[innerTask] <= latestCompletionTime[outerTask])
				{
					setFreeEnergy += freeEnergy[innerTask];
				}
				else
				{
					var additionalEnergy = Math.Min(freeEnergy[innerTask], (long)this.Demands[innerTask] * (latestCompletionTime[outerTask] - earliestStartTime[innerTask]));

					if (candidateTask == -1 || additionalEnergy > candidateAdditionalEnergy)
					{
						candidateTask = innerTask;
						candidateAdditionalEnergy = additionalEnergy;
					}
				}

				if (candidateTask == -1)
					continue;

				var reserve = (long)this.Capacity * (latestCompletionTime[outerTask] - earliestStartTime[innerTask]) - setFreeEnergy - (profileEnergyAtLct[outerTask] - profileEnergyAtEst[innerTask]);

				if (reserve >= candidateAdditionalEnergy)
					continue;

				var compulsoryStart = latestCompletionTime[candidateTask] - this.Durations[candidateTask];
				var compulsoryEnd = earliestStartTime[candidateTask] + this.Durations[candidateTask];
				var mandatoryOverlap = Math.Max(0, Math.Min(latestCompletionTime[outerTask], compulsoryEnd) - Math.Max(earliestStartTime[innerTask], compulsoryStart));
				var newLowerBound = latestCompletionTime[outerTask] - mandatoryOverlap - FloorDiv(reserve, this.Demands[candidateTask]);
				newBounds[candidateTask] = Math.Max(newBounds[candidateTask], newLowerBound);
			}
		}

		for (var i = 0; i < taskCount; ++i)
		{
			if (newBounds[i] <= earliestStartTime[i])
				continue;

			if (this.GenerateReasons)
				this.LastReason = CollectReasonForTasks(Enumerable.Range(0, taskCount));

			if (ApplyNewLowerBound(i, newBounds[i], ref result))
				return ConstraintOperationResult.Violated;
		}

		return result;
	}

	private ConstraintOperationResult TimetableEdgeFindingBackward(int taskCount)
	{
		var result = ConstraintOperationResult.Undecided;
		var earliestStartTime = this.TimetableEarliestStartTimes;
		var latestCompletionTime = this.TimetableLatestCompletionTimes;
		var freeEnergy = this.TimetableFreeEnergies;
		var profileEnergyAtEst = this.TimetableProfileEnergyAtEst;
		var profileEnergyAtLct = this.TimetableProfileEnergyAtLct;
		var newBounds = this.TimetableNewBounds;

		this.TimetableSortedTasks.Clear();
		this.TimetableSortedTasks.AddRange(this.TimetableFreeTaskIndices);
		this.TimetableSortedTasks.Sort((a, b) => latestCompletionTime[a].CompareTo(latestCompletionTime[b]));

		for (var i = 0; i < taskCount; ++i)
			newBounds[i] = this.Starts[i].Domain.UpperBound;

		foreach (var outerTask in this.TimetableFreeTaskIndices)
		{
			var setFreeEnergy = 0L;
			var candidateTask = -1;
			var candidateAdditionalEnergy = 0L;

			foreach (var innerTask in this.TimetableSortedTasks)
			{
				if (latestCompletionTime[innerTask] <= earliestStartTime[outerTask])
					continue;

				if (earliestStartTime[innerTask] >= earliestStartTime[outerTask])
				{
					setFreeEnergy += freeEnergy[innerTask];
				}
				else
				{
					var additionalEnergy = Math.Min(freeEnergy[innerTask], (long)this.Demands[innerTask] * (latestCompletionTime[innerTask] - earliestStartTime[outerTask]));

					if (candidateTask == -1 || additionalEnergy > candidateAdditionalEnergy)
					{
						candidateTask = innerTask;
						candidateAdditionalEnergy = additionalEnergy;
					}
				}

				if (candidateTask == -1)
					continue;

				var reserve = (long)this.Capacity * (latestCompletionTime[innerTask] - earliestStartTime[outerTask]) - setFreeEnergy - (profileEnergyAtLct[innerTask] - profileEnergyAtEst[outerTask]);

				if (reserve >= candidateAdditionalEnergy)
					continue;

				var compulsoryStart = latestCompletionTime[candidateTask] - this.Durations[candidateTask];
				var compulsoryEnd = earliestStartTime[candidateTask] + this.Durations[candidateTask];
				var mandatoryOverlap = Math.Max(0, Math.Min(latestCompletionTime[innerTask], compulsoryEnd) - Math.Max(earliestStartTime[outerTask], compulsoryStart));
				var newUpperBound = earliestStartTime[outerTask] + mandatoryOverlap + FloorDiv(reserve, this.Demands[candidateTask]) - this.Durations[candidateTask];
				newBounds[candidateTask] = Math.Min(newBounds[candidateTask], newUpperBound);
			}
		}

		for (var i = 0; i < taskCount; ++i)
		{
			if (newBounds[i] >= this.Starts[i].Domain.UpperBound)
				continue;

			if (this.GenerateReasons)
				this.LastReason = CollectReasonForTasks(Enumerable.Range(0, taskCount));

			if (ApplyNewUpperBound(i, newBounds[i], ref result))
				return ConstraintOperationResult.Violated;
		}

		return result;
	}

	private ConstraintOperationResult NotFirstOrLastRule(bool notFirst)
	{
		var result = ConstraintOperationResult.Undecided;

		var n = this.Starts.Count;
		for (var k = 0; k < n; ++k)
			this.NotFirstLastSortedTasks[k] = k;

		Array.Sort(this.NotFirstLastSortedTasks, notFirst ? this.NotFirstComparison : this.NotLastComparison);

		for (var i = 0; i < n; ++i)
		{
			if (this.Starts[i].Instantiated())
				continue;

			var taskBound = notFirst
				? this.Starts[i].Domain.LowerBound + this.Durations[i]
				: this.Starts[i].Domain.UpperBound;

			var setEnergy = 0;
			var maxLatestCompletion = int.MinValue;
			var minEarliestCompletion = int.MaxValue;
			var minEarliestStart = int.MaxValue;
			var maxLatestStart = int.MinValue;

			var contributors = this.GenerateReasons ? new List<int>() : null;

			for (var si = 0; si < n; ++si)
			{
				var j = this.NotFirstLastSortedTasks[si];

				if (j == i)
					continue;

				contributors?.Add(j);
				setEnergy += this.Durations[j] * this.Demands[j];

				if (notFirst)
				{
					maxLatestCompletion = Math.Max(maxLatestCompletion, this.Starts[j].Domain.UpperBound + this.Durations[j]);
					minEarliestCompletion = Math.Min(minEarliestCompletion, this.Starts[j].Domain.LowerBound + this.Durations[j]);

					if (maxLatestCompletion <= taskBound || setEnergy <= (maxLatestCompletion - taskBound) * this.Capacity)
						continue;

					SetReasonIfEnabled(contributors, i);

					if (ApplyNewLowerBound(i, minEarliestCompletion, ref result))
						return ConstraintOperationResult.Violated;
				}
				else
				{
					minEarliestStart = Math.Min(minEarliestStart, this.Starts[j].Domain.LowerBound);
					maxLatestStart = Math.Max(maxLatestStart, this.Starts[j].Domain.UpperBound);

					if (minEarliestStart >= taskBound || setEnergy <= (taskBound - minEarliestStart) * this.Capacity)
						continue;

					SetReasonIfEnabled(contributors, i);

					if (ApplyNewUpperBound(i, maxLatestStart - this.Durations[i], ref result))
						return ConstraintOperationResult.Violated;
				}
			}
		}

		return result;
	}

	public bool StateChanged()
	{
		for (var i = 0; i < this.Starts.Count; ++i)
		{
			if (this.Starts[i].Generation != this.GenerationArray[i])
				return true;
		}

		return false;
	}
}
