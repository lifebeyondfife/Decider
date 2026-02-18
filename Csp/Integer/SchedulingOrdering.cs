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

	public IVariable<int> SelectVariable(LinkedList<IVariable<int>> list)
	{
		LinkedListNode<IVariable<int>>? best = null;
		var bestIsScheduling = false;
		var bestEst = int.MaxValue;
		var bestDemand = -1;
		var bestSize = int.MaxValue;

		var node = list.First;
		while (node != null)
		{
			var isScheduling = this.demandByVariable.TryGetValue(node.Value, out var demand);
			var vi = (VariableInteger) node.Value;
			var est = vi.Domain.LowerBound;
			var size = vi.Size();

			if (best == null || IsBetter(isScheduling, est, demand, size,
				bestIsScheduling, bestEst, bestDemand, bestSize))
			{
				best = node;
				bestIsScheduling = isScheduling;
				bestEst = est;
				bestDemand = demand;
				bestSize = size;
			}

			node = node.Next;
		}

		list.Remove(best!);
		return best!.Value;
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
