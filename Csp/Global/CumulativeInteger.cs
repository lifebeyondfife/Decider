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

	public CumulativeInteger(IList<VariableInteger> starts, IList<int> durations, IList<int> demands, int capacity)
	{
		if (starts.Count != durations.Count || starts.Count != demands.Count)
			throw new ArgumentException("starts, durations, and demands must have the same length");

		if (capacity < 0)
			throw new ArgumentException("capacity must be non-negative");

		this.Starts = starts;
		this.Durations = durations;
		this.Demands = demands;
		this.Capacity = capacity;
		this.GenerationArray = Enumerable.Repeat(0, starts.Count).ToList();
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

			foreach (var time in profile.Keys)
			{
				if (profile[time] <= this.Capacity)
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

				var valuesToRemove = new List<int>();

				foreach (var candidateStart in this.Starts[i].Domain)
				{
					if (IsInfeasibleWithProfile(i, candidateStart, profile))
						valuesToRemove.Add(candidateStart);
				}

				foreach (var value in valuesToRemove)
				{
					this.Starts[i].Remove(value, this.Depth, out DomainOperationResult domainResult);

					if (domainResult == DomainOperationResult.EmptyDomain)
					{
						if (this.GenerateReasons)
							this.LastReason = ReasonForTimetableFilter(i, value, profile);

						result = ConstraintOperationResult.Violated;
						return;
					}

					if (domainResult == DomainOperationResult.RemoveSuccessful)
					{
						result = ConstraintOperationResult.Propagated;
						propagationOccurred = true;
					}
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

	private Dictionary<int, int> BuildCompulsoryProfile()
	{
		var profile = new Dictionary<int, int>();

		for (var i = 0; i < this.Starts.Count; ++i)
		{
			if (!GetCompulsoryPart(i, out var compulsoryStart, out var compulsoryEnd))
				continue;

			for (var t = compulsoryStart; t < compulsoryEnd; ++t)
			{
				if (!profile.ContainsKey(t))
					profile[t] = 0;

				profile[t] += this.Demands[i];
			}
		}

		return profile;
	}

	private bool IsInfeasibleWithProfile(int taskIndex, int candidateStart, Dictionary<int, int> profile)
	{
		var end = candidateStart + this.Durations[taskIndex];
		var demand = this.Demands[taskIndex];

		GetCompulsoryPart(taskIndex, out var compulsoryStart, out var compulsoryEnd);

		for (var t = candidateStart; t < end; ++t)
		{
			if (!profile.TryGetValue(t, out var profileDemand))
				profileDemand = 0;

			var taskInCompulsory = t >= compulsoryStart && t < compulsoryEnd;
			var totalDemand = taskInCompulsory ? profileDemand : profileDemand + demand;

			if (totalDemand > this.Capacity)
				return true;
		}

		return false;
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

	private IList<BoundReason> ReasonForTimetableFilter(int taskIndex, int candidateStart, Dictionary<int, int> profile)
	{
		var end = candidateStart + this.Durations[taskIndex];
		var demand = this.Demands[taskIndex];

		GetCompulsoryPart(taskIndex, out var compulsoryStart, out var compulsoryEnd);

		for (var t = candidateStart; t < end; ++t)
		{
			profile.TryGetValue(t, out var profileDemand);

			var taskInCompulsory = t >= compulsoryStart && t < compulsoryEnd;
			var totalDemand = taskInCompulsory ? profileDemand : profileDemand + demand;

			if (totalDemand <= this.Capacity)
				continue;

			return ReasonForProfileOverload(t);
		}

		return new List<BoundReason>();
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

	private ConstraintOperationResult EdgeFinding(bool forward)
	{
		var result = ConstraintOperationResult.Undecided;

		var orderedTasks = forward
			? Enumerable.Range(0, this.Starts.Count).OrderBy(i => this.Starts[i].Domain.UpperBound + this.Durations[i]).ToList()
			: Enumerable.Range(0, this.Starts.Count).OrderByDescending(i => this.Starts[i].Domain.LowerBound).ToList();

		for (var i = 0; i < this.Starts.Count; ++i)
		{
			if (this.Starts[i].Instantiated())
				continue;

			var taskBound = forward
				? this.Starts[i].Domain.LowerBound
				: this.Starts[i].Domain.UpperBound + this.Durations[i];
			var taskEnergy = this.Durations[i] * this.Demands[i];

			var cumulativeEnergy = 0;
			var minEarliestStart = int.MaxValue;
			var maxLatestCompletion = int.MinValue;

			var contributors = this.GenerateReasons ? new List<int>() : null;

			foreach (var j in orderedTasks)
			{
				if (j == i)
					continue;

				contributors?.Add(j);
				cumulativeEnergy += this.Durations[j] * this.Demands[j];
				minEarliestStart = Math.Min(minEarliestStart, this.Starts[j].Domain.LowerBound);
				maxLatestCompletion = Math.Max(maxLatestCompletion, this.Starts[j].Domain.UpperBound + this.Durations[j]);

				var windowStart = forward ? Math.Min(minEarliestStart, taskBound) : minEarliestStart;
				var windowEnd = forward ? maxLatestCompletion : Math.Max(maxLatestCompletion, taskBound);

				if (cumulativeEnergy + taskEnergy <= (windowEnd - windowStart) * this.Capacity)
					continue;

				SetReasonIfEnabled(contributors, i);

				if (forward)
				{
					var newLowerBound = Math.Max(maxLatestCompletion, minEarliestStart + (int)Math.Ceiling((double)cumulativeEnergy / this.Capacity));
					if (ApplyNewLowerBound(i, newLowerBound, ref result))
						return ConstraintOperationResult.Violated;
				}
				else
				{
					var newUpperBound = Math.Min(minEarliestStart, maxLatestCompletion - (int)Math.Ceiling((double)cumulativeEnergy / this.Capacity)) - this.Durations[i];
					if (ApplyNewUpperBound(i, newUpperBound, ref result))
						return ConstraintOperationResult.Violated;
				}
			}
		}

		return result;
	}

	private ConstraintOperationResult NotFirstOrLastRule(bool notFirst)
	{
		var result = ConstraintOperationResult.Undecided;

		var orderedTasks = notFirst
			? Enumerable.Range(0, this.Starts.Count).OrderBy(i => this.Starts[i].Domain.UpperBound + this.Durations[i]).ToList()
			: Enumerable.Range(0, this.Starts.Count).OrderByDescending(i => this.Starts[i].Domain.LowerBound).ToList();

		for (var i = 0; i < this.Starts.Count; ++i)
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

			foreach (var j in orderedTasks)
			{
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
		return this.Starts.Where((t, i) => t.Generation != this.GenerationArray[i]).Any();
	}
}
