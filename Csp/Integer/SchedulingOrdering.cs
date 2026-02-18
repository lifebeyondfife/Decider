/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System.Collections.Generic;

using Decider.Csp.BaseTypes;

namespace Decider.Csp.Integer;

public class SchedulingOrdering : IVariableOrderingHeuristic<int>, IValueOrderingHeuristic<int>
{
	private readonly Dictionary<IVariable<int>, int> demandByVariable;
	private readonly IList<VariableInteger> starts;
	private readonly IList<int> durations;
	private readonly IList<int> demands;
	private readonly int capacity;

	public SchedulingOrdering(IList<VariableInteger> starts, IList<int> durations, IList<int> demands, int capacity)
	{
		this.starts = starts;
		this.durations = durations;
		this.demands = demands;
		this.capacity = capacity;

		this.demandByVariable = new Dictionary<IVariable<int>, int>(starts.Count);
		for (var i = 0; i < starts.Count; ++i)
			this.demandByVariable[starts[i]] = demands[i];
	}

	public int SelectVariableIndex(IList<IVariable<int>> variables)
	{
		var bestIndex = 0;
		var bestIsScheduling = this.demandByVariable.TryGetValue(variables[0], out var bestDemand);
		var bestEst = ((VariableInteger) variables[0]).Domain.LowerBound;
		var bestSize = variables[0].Size();

		for (var i = 1; i < variables.Count; ++i)
		{
			var isScheduling = this.demandByVariable.TryGetValue(variables[i], out var demand);
			var candidate = (VariableInteger) variables[i];
			var est = candidate.Domain.LowerBound;
			var size = candidate.Size();

			if (!IsBetter(isScheduling, est, demand, size, bestIsScheduling, bestEst, bestDemand, bestSize))
				continue;

			bestIndex = i;
			bestIsScheduling = isScheduling;
			bestEst = est;
			bestDemand = demand;
			bestSize = size;
		}

		return bestIndex;
	}

	public int SelectValue(IVariable<int> variable)
	{
		var vi = (VariableInteger) variable;

		if (!this.demandByVariable.TryGetValue(variable, out var taskDemand))
			return vi.Domain.LowerBound;

		var taskIndex = -1;
		for (var i = 0; i < this.starts.Count; ++i)
		{
			if (ReferenceEquals(this.starts[i], variable))
			{
				taskIndex = i;
				break;
			}
		}

		if (taskIndex < 0)
			return vi.Domain.LowerBound;

		var taskDuration = this.durations[taskIndex];
		var profile = BuildCurrentProfile(taskIndex);

		foreach (var candidateStart in vi.Domain)
		{
			if (FitsInProfile(candidateStart, taskDuration, taskDemand, profile))
				return candidateStart;
		}

		return vi.Domain.LowerBound;
	}

	private Dictionary<int, int> BuildCurrentProfile(int excludeTask)
	{
		var profile = new Dictionary<int, int>();

		for (var i = 0; i < this.starts.Count; ++i)
		{
			if (i == excludeTask)
				continue;

			if (this.starts[i].Instantiated())
			{
				var start = this.starts[i].InstantiatedValue;
				var end = start + this.durations[i];

				for (var t = start; t < end; ++t)
				{
					if (!profile.ContainsKey(t))
						profile[t] = 0;

					profile[t] += this.demands[i];
				}

				continue;
			}

			var est = this.starts[i].Domain.LowerBound;
			var lst = this.starts[i].Domain.UpperBound;
			var duration = this.durations[i];

			if (lst >= est + duration)
				continue;

			var compulsoryStart = lst;
			var compulsoryEnd = est + duration;

			for (var t = compulsoryStart; t < compulsoryEnd; ++t)
			{
				if (!profile.ContainsKey(t))
					profile[t] = 0;

				profile[t] += this.demands[i];
			}
		}

		return profile;
	}

	private bool FitsInProfile(int candidateStart, int duration, int demand, Dictionary<int, int> profile)
	{
		var end = candidateStart + duration;

		for (var t = candidateStart; t < end; ++t)
		{
			profile.TryGetValue(t, out var currentUsage);

			if (currentUsage + demand > this.capacity)
				return false;
		}

		return true;
	}

	private static bool IsBetter(bool isScheduling, int est, int demand, int size,
		bool bestIsScheduling, int bestEst, int bestDemand, int bestSize)
	{
		if (isScheduling && !bestIsScheduling)
			return true;

		if (!isScheduling && bestIsScheduling)
			return false;

		if (isScheduling)
		{
			if (est < bestEst)
				return true;

			if (est > bestEst)
				return false;

			if (demand > bestDemand)
				return true;

			if (demand < bestDemand)
				return false;

			return size < bestSize;
		}

		return size < bestSize;
	}
}
