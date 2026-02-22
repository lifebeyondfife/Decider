/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System;
using System.Collections.Generic;

using Decider.Csp.BaseTypes;
using Decider.Csp.Integer;

namespace Decider.Csp.Global;

public class DisjunctiveInteger : IConstraint<int>, IExplainableConstraint
{
	private IList<VariableInteger> Starts { get; set; }

	public IReadOnlyList<IVariable<int>> Variables => (IReadOnlyList<VariableInteger>)this.Starts;
	private IList<int> Durations { get; set; }
	private int[] GenerationArray { get; set; }

	public int FailureWeight { get; set; }

	private int[] EarliestStartTimes { get; set; }
	private int[] LatestCompletionTimes { get; set; }
	private int[] NewLowerBounds { get; set; }
	private int[] NewUpperBounds { get; set; }
	private int[] SortedByLct { get; set; }
	private int[] SortedByEstDesc { get; set; }
	private Comparison<int> LctComparison { get; set; }
	private Comparison<int> EstDescComparison { get; set; }

	public DisjunctiveInteger(IList<VariableInteger> starts, IList<int> durations)
	{
		this.Starts = starts;
		this.Durations = durations;

		if (this.Starts.Count != this.Durations.Count)
			throw new ArgumentException("starts and durations must have the same length");

		var n = this.Starts.Count;
		this.GenerationArray = new int[n];
		for (var i = 0; i < n; ++i)
			this.GenerationArray[i] = -1;

		this.EarliestStartTimes = new int[n];
		this.LatestCompletionTimes = new int[n];
		this.NewLowerBounds = new int[n];
		this.NewUpperBounds = new int[n];
		this.SortedByLct = new int[n];
		this.SortedByEstDesc = new int[n];

		for (var i = 0; i < n; ++i)
		{
			this.SortedByLct[i] = i;
			this.SortedByEstDesc[i] = i;
		}

		var lctArray = this.LatestCompletionTimes;
		var estArray = this.EarliestStartTimes;
		this.LctComparison = (a, b) => lctArray[a].CompareTo(lctArray[b]);
		this.EstDescComparison = (a, b) => estArray[b].CompareTo(estArray[a]);
	}

	public void Check(out ConstraintOperationResult result)
	{
		for (var i = 0; i < this.Starts.Count; ++i)
		{
			this.GenerationArray[i] = this.Starts[i].Generation;

			if (!this.Starts[i].Instantiated())
			{
				result = ConstraintOperationResult.Undecided;
				return;
			}
		}

		for (var i = 0; i < this.Starts.Count; ++i)
		{
			var startI = this.Starts[i].InstantiatedValue;
			var endI = startI + this.Durations[i];

			for (var j = i + 1; j < this.Starts.Count; ++j)
			{
				var startJ = this.Starts[j].InstantiatedValue;
				var endJ = startJ + this.Durations[j];

				if (startI < endJ && startJ < endI)
				{
					result = ConstraintOperationResult.Violated;
					return;
				}
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

			RefreshBounds();

			if (ApplySubResult(OverloadCheck(), ref result, ref propagationOccurred))
				return;

			if (ApplySubResult(DetectablePrecedences(), ref result, ref propagationOccurred))
				return;

			if (ApplySubResult(NotFirstNotLast(true), ref result, ref propagationOccurred))
				return;

			if (ApplySubResult(NotFirstNotLast(false), ref result, ref propagationOccurred))
				return;

			if (ApplySubResult(EdgeFinding(true), ref result, ref propagationOccurred))
				return;

			if (ApplySubResult(EdgeFinding(false), ref result, ref propagationOccurred))
				return;
		}

		for (var i = 0; i < this.Starts.Count; ++i)
			this.GenerationArray[i] = this.Starts[i].Generation;
	}

	private void RefreshBounds()
	{
		var n = this.Starts.Count;

		for (var i = 0; i < n; ++i)
		{
			this.EarliestStartTimes[i] = this.Starts[i].Domain.LowerBound;
			this.LatestCompletionTimes[i] = this.Starts[i].Domain.UpperBound + this.Durations[i];
			this.SortedByLct[i] = i;
			this.SortedByEstDesc[i] = i;
		}

		Array.Sort(this.SortedByLct, this.LctComparison);
		Array.Sort(this.SortedByEstDesc, this.EstDescComparison);
	}

	private ConstraintOperationResult OverloadCheck()
	{
		var n = this.Starts.Count;

		for (var i = 0; i < n; ++i)
		{
			for (var j = i + 1; j < n; ++j)
			{
				var minStart = Math.Min(this.EarliestStartTimes[i], this.EarliestStartTimes[j]);
				var maxEnd = Math.Max(this.LatestCompletionTimes[i], this.LatestCompletionTimes[j]);

				if (this.Durations[i] + this.Durations[j] > maxEnd - minStart)
					return ConstraintOperationResult.Violated;
			}
		}

		return ConstraintOperationResult.Undecided;
	}

	private ConstraintOperationResult DetectablePrecedences()
	{
		var result = ConstraintOperationResult.Undecided;
		var n = this.Starts.Count;

		for (var i = 0; i < n; ++i)
			this.NewLowerBounds[i] = this.EarliestStartTimes[i];

		for (var i = 0; i < n; ++i)
			this.NewUpperBounds[i] = this.Starts[i].Domain.UpperBound;

		for (var i = 0; i < n; ++i)
		{
			for (var j = 0; j < n; ++j)
			{
				if (i == j)
					continue;

				if (this.EarliestStartTimes[i] + this.Durations[i] + this.Durations[j] > this.LatestCompletionTimes[j])
				{
					this.NewLowerBounds[i] = Math.Max(this.NewLowerBounds[i], this.EarliestStartTimes[j] + this.Durations[j]);
					this.NewUpperBounds[j] = Math.Min(this.NewUpperBounds[j], this.Starts[i].Domain.UpperBound - this.Durations[j]);
				}
			}
		}

		for (var i = 0; i < n; ++i)
		{
			if (this.NewLowerBounds[i] > this.EarliestStartTimes[i])
			{
				if (ApplyNewLowerBound(i, this.NewLowerBounds[i], ref result))
					return ConstraintOperationResult.Violated;
			}

			if (this.NewUpperBounds[i] < this.Starts[i].Domain.UpperBound)
			{
				if (ApplyNewUpperBound(i, this.NewUpperBounds[i], ref result))
					return ConstraintOperationResult.Violated;
			}
		}

		return result;
	}

	private ConstraintOperationResult NotFirstNotLast(bool notFirst)
	{
		var result = ConstraintOperationResult.Undecided;
		var n = this.Starts.Count;
		var sorted = notFirst ? this.SortedByLct : this.SortedByEstDesc;

		for (var i = 0; i < n; ++i)
		{
			if (this.Starts[i].Instantiated())
				continue;

			if (notFirst)
			{
				var minEct = int.MaxValue;
				var totalDuration = 0;
				var maxLct = int.MinValue;

				for (var si = 0; si < n; ++si)
				{
					var j = sorted[si];
					if (j == i)
						continue;

					totalDuration += this.Durations[j];
					maxLct = Math.Max(maxLct, this.LatestCompletionTimes[j]);
					minEct = Math.Min(minEct, this.EarliestStartTimes[j] + this.Durations[j]);

					if (maxLct <= this.EarliestStartTimes[i] + this.Durations[i])
						continue;

					if (totalDuration <= maxLct - this.EarliestStartTimes[i] - this.Durations[i])
						continue;

					if (ApplyNewLowerBound(i, minEct, ref result))
						return ConstraintOperationResult.Violated;

					break;
				}
			}
			else
			{
				var maxLst = int.MinValue;
				var totalDuration = 0;
				var minEst = int.MaxValue;

				for (var si = 0; si < n; ++si)
				{
					var j = sorted[si];
					if (j == i)
						continue;

					totalDuration += this.Durations[j];
					minEst = Math.Min(minEst, this.EarliestStartTimes[j]);
					maxLst = Math.Max(maxLst, this.Starts[j].Domain.UpperBound);

					if (minEst >= this.Starts[i].Domain.UpperBound)
						continue;

					if (totalDuration <= this.Starts[i].Domain.UpperBound - minEst)
						continue;

					if (ApplyNewUpperBound(i, maxLst - this.Durations[i], ref result))
						return ConstraintOperationResult.Violated;

					break;
				}
			}
		}

		return result;
	}

	private ConstraintOperationResult EdgeFinding(bool forward)
	{
		var n = this.Starts.Count;

		if (n < 2)
			return ConstraintOperationResult.Undecided;

		if (forward)
			return EdgeFindingForward(n);

		return EdgeFindingBackward(n);
	}

	private ConstraintOperationResult EdgeFindingForward(int n)
	{
		var result = ConstraintOperationResult.Undecided;

		for (var i = 0; i < n; ++i)
			this.NewLowerBounds[i] = this.EarliestStartTimes[i];

		for (var outerIdx = 0; outerIdx < n; ++outerIdx)
		{
			var outerLct = this.LatestCompletionTimes[this.SortedByLct[outerIdx]];
			var setEnergy = 0;
			var setEst = int.MaxValue;

			for (var innerIdx = outerIdx; innerIdx >= 0; --innerIdx)
			{
				var inner = this.SortedByLct[innerIdx];
				setEnergy += this.Durations[inner];
				setEst = Math.Min(setEst, this.EarliestStartTimes[inner]);

				if (setEst + setEnergy > outerLct)
				{
					return ConstraintOperationResult.Violated;
				}
			}

			for (var candidateIdx = 0; candidateIdx < n; ++candidateIdx)
			{
				if (this.LatestCompletionTimes[candidateIdx] <= outerLct)
					continue;

				var candidateEst = this.EarliestStartTimes[candidateIdx];
				if (candidateEst >= outerLct)
					continue;

				var energy = 0;
				var est = candidateEst;

				for (var innerIdx = outerIdx; innerIdx >= 0; --innerIdx)
				{
					var inner = this.SortedByLct[innerIdx];
					energy += this.Durations[inner];
					est = Math.Min(est, this.EarliestStartTimes[inner]);
				}

				if (est + energy + this.Durations[candidateIdx] > outerLct)
					this.NewLowerBounds[candidateIdx] = Math.Max(this.NewLowerBounds[candidateIdx], est + energy);
			}
		}

		for (var i = 0; i < n; ++i)
		{
			if (this.NewLowerBounds[i] <= this.EarliestStartTimes[i])
				continue;

if (ApplyNewLowerBound(i, this.NewLowerBounds[i], ref result))
				return ConstraintOperationResult.Violated;
		}

		return result;
	}

	private ConstraintOperationResult EdgeFindingBackward(int n)
	{
		var result = ConstraintOperationResult.Undecided;

		for (var i = 0; i < n; ++i)
			this.NewUpperBounds[i] = this.Starts[i].Domain.UpperBound;

		for (var outerIdx = 0; outerIdx < n; ++outerIdx)
		{
			var outerEst = this.EarliestStartTimes[this.SortedByEstDesc[outerIdx]];
			var setEnergy = 0;
			var setLct = int.MinValue;

			for (var innerIdx = outerIdx; innerIdx >= 0; --innerIdx)
			{
				var inner = this.SortedByEstDesc[innerIdx];
				setEnergy += this.Durations[inner];
				setLct = Math.Max(setLct, this.LatestCompletionTimes[inner]);

				if (setLct - setEnergy < outerEst)
				{
					return ConstraintOperationResult.Violated;
				}
			}

			for (var candidateIdx = 0; candidateIdx < n; ++candidateIdx)
			{
				if (this.EarliestStartTimes[candidateIdx] >= outerEst)
					continue;

				var candidateLct = this.LatestCompletionTimes[candidateIdx];
				if (candidateLct <= outerEst)
					continue;

				var energy = 0;
				var lct = candidateLct;

				for (var innerIdx = outerIdx; innerIdx >= 0; --innerIdx)
				{
					var inner = this.SortedByEstDesc[innerIdx];
					energy += this.Durations[inner];
					lct = Math.Max(lct, this.LatestCompletionTimes[inner]);
				}

				if (lct - energy - this.Durations[candidateIdx] < outerEst)
					this.NewUpperBounds[candidateIdx] = Math.Min(this.NewUpperBounds[candidateIdx], lct - energy - this.Durations[candidateIdx]);
			}
		}

		for (var i = 0; i < n; ++i)
		{
			if (this.NewUpperBounds[i] >= this.Starts[i].Domain.UpperBound)
				continue;

if (ApplyNewUpperBound(i, this.NewUpperBounds[i], ref result))
				return ConstraintOperationResult.Violated;
		}

		return result;
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

	private IList<BoundReason> CollectReasonForTasks(int task1, int task2)
	{
		return new List<BoundReason>
		{
			new(this.Starts[task1].VariableId, true, this.Starts[task1].Domain.LowerBound),
			new(this.Starts[task1].VariableId, false, this.Starts[task1].Domain.UpperBound),
			new(this.Starts[task2].VariableId, true, this.Starts[task2].Domain.LowerBound),
			new(this.Starts[task2].VariableId, false, this.Starts[task2].Domain.UpperBound),
		};
	}

	private IList<BoundReason> CollectAllReasons()
	{
		var reasons = new List<BoundReason>(this.Starts.Count * 2);

		for (var j = 0; j < this.Starts.Count; ++j)
		{
			reasons.Add(new BoundReason(this.Starts[j].VariableId, true, this.Starts[j].Domain.LowerBound));
			reasons.Add(new BoundReason(this.Starts[j].VariableId, false, this.Starts[j].Domain.UpperBound));
		}

		return reasons;
	}

	public void Explain(int variableId, bool isLowerBound, int boundValue, IList<BoundReason> result)
	{
		for (var j = 0; j < this.Starts.Count; ++j)
		{
			result.Add(new BoundReason(this.Starts[j].VariableId, true, this.Starts[j].Domain.LowerBound));
			result.Add(new BoundReason(this.Starts[j].VariableId, false, this.Starts[j].Domain.UpperBound));
		}
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
