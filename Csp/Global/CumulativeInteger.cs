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
	private readonly IList<VariableInteger> starts;
	private readonly IList<int> durations;
	private readonly IList<int> demands;
	private readonly int capacity;
	private readonly IList<int> generationArray;

	private IState<int>? State { get; set; }
	private int Depth
	{
		get
		{
			if (this.State == null)
				this.State = this.starts[0].State;

			return this.State!.Depth;
		}
	}

	public CumulativeInteger(IList<VariableInteger> starts, IList<int> durations, IList<int> demands, int capacity)
	{
		if (starts.Count != durations.Count || starts.Count != demands.Count)
			throw new ArgumentException("starts, durations, and demands must have the same length");

		if (capacity < 0)
			throw new ArgumentException("capacity must be non-negative");

		this.starts = starts;
		this.durations = durations;
		this.demands = demands;
		this.capacity = capacity;
		this.generationArray = Enumerable.Repeat(0, starts.Count).ToList();
	}

	public void Check(out ConstraintOperationResult result)
	{
		for (var i = 0; i < this.starts.Count; ++i)
			this.generationArray[i] = this.starts[i].Generation;

		if (this.starts.Any(s => !s.Instantiated()))
		{
			result = ConstraintOperationResult.Undecided;
			return;
		}

		var profile = new Dictionary<int, int>();
		for (var i = 0; i < this.starts.Count; ++i)
		{
			var start = this.starts[i].InstantiatedValue;
			var end = start + this.durations[i];
			for (var t = start; t < end; ++t)
			{
				if (!profile.ContainsKey(t))
					profile[t] = 0;
				profile[t] += this.demands[i];
			}
		}

		foreach (var demand in profile.Values)
		{
			if (demand > this.capacity)
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
		var changed = true;

		while (changed)
		{
			changed = false;

			if (CheckResourceProfile())
			{
				result = ConstraintOperationResult.Violated;
				return;
			}

			for (var i = 0; i < this.starts.Count; ++i)
			{
				if (this.starts[i].Instantiated())
					continue;

				var valuesToRemove = new List<int>();

				foreach (var candidateStart in this.starts[i].Domain)
				{
					if (IsInfeasible(i, candidateStart))
						valuesToRemove.Add(candidateStart);
				}

				foreach (var value in valuesToRemove)
				{
					this.starts[i].Remove(value, this.Depth, out DomainOperationResult domainResult);

					if (domainResult == DomainOperationResult.EmptyDomain)
					{
						result = ConstraintOperationResult.Violated;
						return;
					}

					if (domainResult == DomainOperationResult.RemoveSuccessful)
					{
						result = ConstraintOperationResult.Propagated;
						changed = true;
					}
				}
			}
		}
	}

	private bool CheckResourceProfile()
	{
		var profile = new Dictionary<int, int>();

		for (var i = 0; i < this.starts.Count; ++i)
		{
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

				if (profile[t] > this.capacity)
					return true;
			}
		}

		return false;
	}

	private bool IsInfeasible(int taskIndex, int candidateStart)
	{
		var end = candidateStart + this.durations[taskIndex];
		var demand = this.demands[taskIndex];

		for (var t = candidateStart; t < end; ++t)
		{
			var totalDemand = demand;

			for (var j = 0; j < this.starts.Count; ++j)
			{
				if (j == taskIndex)
					continue;

				var est = this.starts[j].Domain.LowerBound;
				var lst = this.starts[j].Domain.UpperBound;
				var duration = this.durations[j];

				if (lst >= est + duration)
					continue;

				var compulsoryStart = lst;
				var compulsoryEnd = est + duration;

				if (t >= compulsoryStart && t < compulsoryEnd)
					totalDemand += this.demands[j];
			}

			if (totalDemand > this.capacity)
				return true;
		}

		return false;
	}

	public bool StateChanged()
	{
		return this.starts.Where((t, i) => t.Generation != this.generationArray[i]).Any();
	}
}
