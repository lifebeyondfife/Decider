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

public class CumulativeInteger : IConstraint
{
	private IList<VariableInteger> Starts { get; set; }
	private IList<int> Durations { get; set; }
	private IList<int> Demands { get; set; }
	private int Capacity { get; set; }
	private IList<int> GenerationArray { get; set; }

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
		var propagationOccurred = true;

		while (propagationOccurred)
		{
			propagationOccurred = false;

			var profile = BuildCompulsoryProfile();

			foreach (var demand in profile.Values)
			{
				if (demand > this.Capacity)
				{
					result = ConstraintOperationResult.Violated;
					return;
				}
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

			var leftToRightResult = EdgeFindingLeftToRight();
			if (leftToRightResult == ConstraintOperationResult.Violated)
			{
				result = ConstraintOperationResult.Violated;
				return;
			}
			if (leftToRightResult == ConstraintOperationResult.Propagated)
			{
				result = ConstraintOperationResult.Propagated;
				propagationOccurred = true;
			}

			var rightToLeftResult = EdgeFindingRightToLeft();
			if (rightToLeftResult == ConstraintOperationResult.Violated)
			{
				result = ConstraintOperationResult.Violated;
				return;
			}
			if (rightToLeftResult == ConstraintOperationResult.Propagated)
			{
				result = ConstraintOperationResult.Propagated;
				propagationOccurred = true;
			}

			var notFirstResult = NotFirstRule();
			if (notFirstResult == ConstraintOperationResult.Violated)
			{
				result = ConstraintOperationResult.Violated;
				return;
			}
			if (notFirstResult == ConstraintOperationResult.Propagated)
			{
				result = ConstraintOperationResult.Propagated;
				propagationOccurred = true;
			}

			var notLastResult = NotLastRule();
			if (notLastResult == ConstraintOperationResult.Violated)
			{
				result = ConstraintOperationResult.Violated;
				return;
			}
			if (notLastResult == ConstraintOperationResult.Propagated)
			{
				result = ConstraintOperationResult.Propagated;
				propagationOccurred = true;
			}
		}
	}

	private Dictionary<int, int> BuildCompulsoryProfile()
	{
		var profile = new Dictionary<int, int>();

		for (var i = 0; i < this.Starts.Count; ++i)
		{
			var est = this.Starts[i].Domain.LowerBound;
			var lst = this.Starts[i].Domain.UpperBound;
			var duration = this.Durations[i];

			if (lst >= est + duration)
				continue;

			var compulsoryStart = lst;
			var compulsoryEnd = est + duration;

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

		var est = this.Starts[taskIndex].Domain.LowerBound;
		var lst = this.Starts[taskIndex].Domain.UpperBound;
		var duration = this.Durations[taskIndex];
		var hasCompulsoryPart = lst < est + duration;

		var compulsoryStart = hasCompulsoryPart ? lst : int.MaxValue;
		var compulsoryEnd = hasCompulsoryPart ? est + duration : int.MinValue;

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

	private ConstraintOperationResult EdgeFindingLeftToRight()
	{
		var result = ConstraintOperationResult.Undecided;

		var tasksByLatestCompletion = Enumerable.Range(0, this.Starts.Count)
			.OrderBy(i => this.Starts[i].Domain.UpperBound + this.Durations[i])
			.ToList();

		for (var i = 0; i < this.Starts.Count; ++i)
		{
			if (this.Starts[i].Instantiated())
				continue;

			var taskEarliestStart = this.Starts[i].Domain.LowerBound;
			var taskEnergy = this.Durations[i] * this.Demands[i];

			var cumulativeEnergy = 0;
			var minEarliestStart = int.MaxValue;
			var maxLatestCompletion = int.MinValue;

			foreach (var j in tasksByLatestCompletion)
			{
				if (j == i)
					continue;

				cumulativeEnergy += this.Durations[j] * this.Demands[j];
				minEarliestStart = Math.Min(minEarliestStart, this.Starts[j].Domain.LowerBound);
				maxLatestCompletion = Math.Max(maxLatestCompletion, this.Starts[j].Domain.UpperBound + this.Durations[j]);

				var windowStart = Math.Min(minEarliestStart, taskEarliestStart);
				var windowEnd = maxLatestCompletion;
				var windowCapacity = (windowEnd - windowStart) * this.Capacity;

				if (cumulativeEnergy + taskEnergy > windowCapacity)
				{
					var newLowerBound = maxLatestCompletion;

					if (newLowerBound > this.Starts[i].Domain.UpperBound)
						return ConstraintOperationResult.Violated;

					if (newLowerBound > this.Starts[i].Domain.LowerBound)
					{
						var bounds = new Bounds<int>(newLowerBound, this.Starts[i].Domain.UpperBound);
						this.Starts[i].Propagate(bounds, out var propagateResult);

						if (propagateResult == ConstraintOperationResult.Violated)
							return ConstraintOperationResult.Violated;

						if (propagateResult == ConstraintOperationResult.Propagated)
							result = ConstraintOperationResult.Propagated;
					}
				}
			}
		}

		return result;
	}

	private ConstraintOperationResult EdgeFindingRightToLeft()
	{
		var result = ConstraintOperationResult.Undecided;

		var tasksByEarliestStart = Enumerable.Range(0, this.Starts.Count)
			.OrderByDescending(i => this.Starts[i].Domain.LowerBound)
			.ToList();

		for (var i = 0; i < this.Starts.Count; ++i)
		{
			if (this.Starts[i].Instantiated())
				continue;

			var taskLatestCompletion = this.Starts[i].Domain.UpperBound + this.Durations[i];
			var taskEnergy = this.Durations[i] * this.Demands[i];

			var cumulativeEnergy = 0;
			var minEarliestStart = int.MaxValue;
			var maxLatestCompletion = int.MinValue;

			foreach (var j in tasksByEarliestStart)
			{
				if (j == i)
					continue;

				cumulativeEnergy += this.Durations[j] * this.Demands[j];
				minEarliestStart = Math.Min(minEarliestStart, this.Starts[j].Domain.LowerBound);
				maxLatestCompletion = Math.Max(maxLatestCompletion, this.Starts[j].Domain.UpperBound + this.Durations[j]);

				var windowStart = minEarliestStart;
				var windowEnd = Math.Max(maxLatestCompletion, taskLatestCompletion);
				var windowCapacity = (windowEnd - windowStart) * this.Capacity;

				if (cumulativeEnergy + taskEnergy > windowCapacity)
				{
					var newUpperBound = minEarliestStart - this.Durations[i];

					if (newUpperBound < this.Starts[i].Domain.LowerBound)
						return ConstraintOperationResult.Violated;

					if (newUpperBound < this.Starts[i].Domain.UpperBound)
					{
						var bounds = new Bounds<int>(this.Starts[i].Domain.LowerBound, newUpperBound);
						this.Starts[i].Propagate(bounds, out var propagateResult);

						if (propagateResult == ConstraintOperationResult.Violated)
							return ConstraintOperationResult.Violated;

						if (propagateResult == ConstraintOperationResult.Propagated)
							result = ConstraintOperationResult.Propagated;
					}
				}
			}
		}

		return result;
	}

	private ConstraintOperationResult NotFirstRule()
	{
		var result = ConstraintOperationResult.Undecided;

		var tasksByLatestCompletion = Enumerable.Range(0, this.Starts.Count)
			.OrderBy(i => this.Starts[i].Domain.UpperBound + this.Durations[i])
			.ToList();

		for (var i = 0; i < this.Starts.Count; ++i)
		{
			if (this.Starts[i].Instantiated())
				continue;

			var taskEarliestStart = this.Starts[i].Domain.LowerBound;
			var taskEarliestCompletion = taskEarliestStart + this.Durations[i];

			var setEnergy = 0;
			var maxLatestCompletion = int.MinValue;
			var minEarliestCompletion = int.MaxValue;

			foreach (var j in tasksByLatestCompletion)
			{
				if (j == i)
					continue;

				setEnergy += this.Durations[j] * this.Demands[j];
				maxLatestCompletion = Math.Max(maxLatestCompletion, this.Starts[j].Domain.UpperBound + this.Durations[j]);
				minEarliestCompletion = Math.Min(minEarliestCompletion, this.Starts[j].Domain.LowerBound + this.Durations[j]);

				if (maxLatestCompletion <= taskEarliestCompletion)
					continue;

				var availableWindow = maxLatestCompletion - taskEarliestCompletion;
				var availableCapacity = availableWindow * this.Capacity;

				if (setEnergy > availableCapacity)
				{
					var newLowerBound = minEarliestCompletion;

					if (newLowerBound > this.Starts[i].Domain.UpperBound)
						return ConstraintOperationResult.Violated;

					if (newLowerBound > this.Starts[i].Domain.LowerBound)
					{
						var bounds = new Bounds<int>(newLowerBound, this.Starts[i].Domain.UpperBound);
						this.Starts[i].Propagate(bounds, out var propagateResult);

						if (propagateResult == ConstraintOperationResult.Violated)
							return ConstraintOperationResult.Violated;

						if (propagateResult == ConstraintOperationResult.Propagated)
							result = ConstraintOperationResult.Propagated;
					}
				}
			}
		}

		return result;
	}

	private ConstraintOperationResult NotLastRule()
	{
		var result = ConstraintOperationResult.Undecided;

		var tasksByEarliestStart = Enumerable.Range(0, this.Starts.Count)
			.OrderByDescending(i => this.Starts[i].Domain.LowerBound)
			.ToList();

		for (var i = 0; i < this.Starts.Count; ++i)
		{
			if (this.Starts[i].Instantiated())
				continue;

			var taskLatestCompletion = this.Starts[i].Domain.UpperBound + this.Durations[i];
			var taskLatestStart = this.Starts[i].Domain.UpperBound;

			var setEnergy = 0;
			var minEarliestStart = int.MaxValue;
			var maxLatestStart = int.MinValue;

			foreach (var j in tasksByEarliestStart)
			{
				if (j == i)
					continue;

				setEnergy += this.Durations[j] * this.Demands[j];
				minEarliestStart = Math.Min(minEarliestStart, this.Starts[j].Domain.LowerBound);
				maxLatestStart = Math.Max(maxLatestStart, this.Starts[j].Domain.UpperBound);

				if (minEarliestStart >= taskLatestStart)
					continue;

				var availableWindow = taskLatestStart - minEarliestStart;
				var availableCapacity = availableWindow * this.Capacity;

				if (setEnergy > availableCapacity)
				{
					var newUpperBound = maxLatestStart - this.Durations[i];

					if (newUpperBound < this.Starts[i].Domain.LowerBound)
						return ConstraintOperationResult.Violated;

					if (newUpperBound < this.Starts[i].Domain.UpperBound)
					{
						var bounds = new Bounds<int>(this.Starts[i].Domain.LowerBound, newUpperBound);
						this.Starts[i].Propagate(bounds, out var propagateResult);

						if (propagateResult == ConstraintOperationResult.Violated)
							return ConstraintOperationResult.Violated;

						if (propagateResult == ConstraintOperationResult.Propagated)
							result = ConstraintOperationResult.Propagated;
					}
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
