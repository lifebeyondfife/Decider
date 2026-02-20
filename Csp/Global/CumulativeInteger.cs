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
	private IList<int> GenerationArray { get; set; }

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
		this.GenerationArray = Enumerable.Repeat(0, this.Starts.Count).ToList();
		this.Tree = new ThetaTree();
	}

	public void Check(out ConstraintOperationResult result)
	{
		for (var i = 0; i < this.Starts.Count; ++i)
			this.GenerationArray[i] = this.Starts[i].Generation;

		if (this.Starts.Any(s => !s.Instantiated()))
		{
			result = ConstraintOperationResult.Undecided;
			return;
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

			var profile = BuildCompulsoryProfile();

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

			if (ApplySubResult(EdgeFinding(true), ref result, ref propagationOccurred))
				return;

			if (ApplySubResult(EdgeFinding(false), ref result, ref propagationOccurred))
				return;

			if (ApplySubResult(NotFirstOrLastRule(true), ref result, ref propagationOccurred))
				return;

			if (ApplySubResult(NotFirstOrLastRule(false), ref result, ref propagationOccurred))
				return;
		}
	}

	private bool GetCompulsoryPart(int taskIndex, out int compulsoryStart, out int compulsoryEnd)
	{
		var earliestStartTime = this.Starts[taskIndex].Domain.LowerBound;
		var latestStartTime = this.Starts[taskIndex].Domain.UpperBound;
		var duration = this.Durations[taskIndex];

		if (latestStartTime >= earliestStartTime + duration)
		{
			compulsoryStart = int.MaxValue;
			compulsoryEnd = int.MinValue;
			return false;
		}

		compulsoryStart = latestStartTime;
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

	private static long CeilDiv(long a, long b) =>
		a >= 0 ? (a + b - 1) / b : a / b;

	private ThetaTree Tree { get; set; }

	private ConstraintOperationResult EdgeFinding(bool forward)
	{
		var result = ConstraintOperationResult.Undecided;
		var n = this.Starts.Count;

		if (n < 2)
			return result;

		var treeOrder = forward
			? Enumerable.Range(0, n).OrderBy(j => this.Starts[j].Domain.LowerBound).ToList()
			: Enumerable.Range(0, n).OrderByDescending(j => this.Starts[j].Domain.UpperBound + this.Durations[j]).ToList();

		var rank = new int[n];
		for (var pos = 0; pos < n; ++pos)
			rank[treeOrder[pos]] = pos;

		var maxLct = int.MinValue;
		var maxLctCount = 0;
		var secondMaxLct = int.MinValue;
		var minEst = int.MaxValue;
		var minEstCount = 0;
		var secondMinEst = int.MaxValue;

		for (var j = 0; j < n; ++j)
		{
			var lct = this.Starts[j].Domain.UpperBound + this.Durations[j];
			var est = this.Starts[j].Domain.LowerBound;

			if (lct > maxLct)
			{
				secondMaxLct = maxLct;
				maxLct = lct;
				maxLctCount = 1;
			}
			else if (lct == maxLct)
			{
				maxLctCount++;
			}
			else if (lct > secondMaxLct)
			{
				secondMaxLct = lct;
			}

			if (est < minEst)
			{
				secondMinEst = minEst;
				minEst = est;
				minEstCount = 1;
			}
			else if (est == minEst)
			{
				minEstCount++;
			}
			else if (est < secondMinEst)
			{
				secondMinEst = est;
			}
		}

		this.Tree.Reset(n);

		for (var j = 0; j < n; ++j)
		{
			var energy = (long)this.Durations[j] * this.Demands[j];

			var leafEnv = forward
				? (long)this.Starts[j].Domain.LowerBound * this.Capacity + energy
				: -(long)(this.Starts[j].Domain.UpperBound + this.Durations[j]) * this.Capacity + energy;

			this.Tree.Activate(rank[j], leafEnv, energy);
		}

		for (var i = 0; i < n; ++i)
		{
			if (this.Starts[i].Instantiated())
				continue;

			var energyI = (long)this.Durations[i] * this.Demands[i];
			this.Tree.Deactivate(rank[i]);

			var env = this.Tree.RootEnvelope;
			var envE = this.Tree.RootEnvEnergy;

			var estI = this.Starts[i].Domain.LowerBound;
			var lctI = this.Starts[i].Domain.UpperBound + this.Durations[i];

			var maxLctExcluding = (lctI == maxLct && maxLctCount == 1) ? secondMaxLct : maxLct;
			var minEstExcluding = (estI == minEst && minEstCount == 1) ? secondMinEst : minEst;

			var threshold = forward
				? (long)maxLctExcluding * this.Capacity
				: -(long)minEstExcluding * this.Capacity;

			var leafEnv = forward
				? (long)estI * this.Capacity + energyI
				: -(long)lctI * this.Capacity + energyI;
			this.Tree.Activate(rank[i], leafEnv, energyI);

			var effectiveEnv = env;

			if (forward)
			{
				var minEstS = (env - envE) / this.Capacity;

				if (minEstS > estI)
					effectiveEnv = (long)estI * this.Capacity + envE;
			}
			else
			{
				var maxLctS = -(env - envE) / this.Capacity;

				if (maxLctS < lctI)
					effectiveEnv = -(long)lctI * this.Capacity + envE;
			}

			if (effectiveEnv + energyI <= threshold)
				continue;

			if (this.GenerateReasons)
				this.LastReason = CollectReasonForTasks(Enumerable.Range(0, n));

			if (forward)
			{
				var newLowerBound = (int)((env - envE) / this.Capacity + CeilDiv(envE, this.Capacity));

				if (ApplyNewLowerBound(i, newLowerBound, ref result))
					return ConstraintOperationResult.Violated;
			}
			else
			{
				var newUpperBound = (int)(-(env - envE) / this.Capacity - CeilDiv(envE, this.Capacity)) - this.Durations[i];

				if (ApplyNewUpperBound(i, newUpperBound, ref result))
					return ConstraintOperationResult.Violated;
			}
		}

		return result;
	}

	private ConstraintOperationResult NotFirstOrLastRule(bool notFirst)
	{
		var result = ConstraintOperationResult.Undecided;
		var n = this.Starts.Count;

		if (n < 2)
			return result;

		var orderedTasks = notFirst
			? Enumerable.Range(0, n).OrderBy(j => this.Starts[j].Domain.UpperBound + this.Durations[j]).ToList()
			: Enumerable.Range(0, n).OrderByDescending(j => this.Starts[j].Domain.LowerBound).ToList();

		for (var i = 0; i < n; ++i)
		{
			if (this.Starts[i].Instantiated())
				continue;

			var taskBound = notFirst
				? this.Starts[i].Domain.LowerBound + this.Durations[i]
				: this.Starts[i].Domain.UpperBound;

			var setEnergy = 0L;
			var maxLatestCompletionTime = int.MinValue;
			var minEarliestCompletionTime = int.MaxValue;
			var minEarliestStartTime = int.MaxValue;
			var maxLatestStartTime = int.MinValue;

			var contributors = this.GenerateReasons ? new List<int>() : null;

			foreach (var j in orderedTasks)
			{
				if (j == i)
					continue;

				contributors?.Add(j);
				setEnergy += (long)this.Durations[j] * this.Demands[j];

				if (notFirst)
				{
					maxLatestCompletionTime = Math.Max(maxLatestCompletionTime, this.Starts[j].Domain.UpperBound + this.Durations[j]);
					minEarliestCompletionTime = Math.Min(minEarliestCompletionTime, this.Starts[j].Domain.LowerBound + this.Durations[j]);

					if (maxLatestCompletionTime <= taskBound || setEnergy <= (long)(maxLatestCompletionTime - taskBound) * this.Capacity)
						continue;

					SetReasonIfEnabled(contributors, i);

					if (ApplyNewLowerBound(i, minEarliestCompletionTime, ref result))
						return ConstraintOperationResult.Violated;
				}
				else
				{
					minEarliestStartTime = Math.Min(minEarliestStartTime, this.Starts[j].Domain.LowerBound);
					maxLatestStartTime = Math.Max(maxLatestStartTime, this.Starts[j].Domain.UpperBound);

					if (minEarliestStartTime >= taskBound || setEnergy <= (long)(taskBound - minEarliestStartTime) * this.Capacity)
						continue;

					SetReasonIfEnabled(contributors, i);

					if (ApplyNewUpperBound(i, maxLatestStartTime - this.Durations[i], ref result))
						return ConstraintOperationResult.Violated;
				}
			}
		}

		return result;
	}

	public bool StateChanged()
	{
		return this.Starts.Where((t, i) => t.Generation != this.GenerationArray[i]).Any();
	}
}
