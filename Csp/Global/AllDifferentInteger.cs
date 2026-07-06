/*
  Copyright © Iain McDonald 2010-2026

  This file is part of Decider.
*/
using System;
using System.Collections.Generic;
using System.Linq;

using Decider.Csp.BaseTypes;
using Decider.Csp.Integer;

namespace Decider.Csp.Global;

public class AllDifferentInteger : IBacktrackableConstraint, IConstraint<int>, IExplainableConstraint
{
	private VariableInteger[] VariableList { get; set; } = Array.Empty<VariableInteger>();

	public IReadOnlyList<IVariable<int>> Variables => this.VariableList;
	public int FailureWeight { get; set; }
	private IList<int> GenerationList { get; set; }
	private BipartiteGraph? Graph { get; set; }
	private readonly CycleDetection cycleDetection;
	private readonly Stack<(int Depth, int?[] Matching)> matchingTrail;
	private int?[]? lastMatching;

	private IState<int>? State { get; set; }
	private int Depth
	{
		get
		{
			if (this.State == null)
				this.State = this.VariableList[0].State;

			return this.State!.Depth;
		}
	}

	public AllDifferentInteger(IEnumerable<VariableInteger> variables)
	{
		this.VariableList = variables.ToArray();
		this.GenerationList = new int[this.VariableList.Length];
		this.cycleDetection = new CycleDetection();
		this.matchingTrail = new Stack<(int Depth, int?[] Matching)>();
	}

	public void Check(out ConstraintOperationResult result)
	{
		for (var i = 0; i < this.VariableList.Length; ++i)
			this.GenerationList[i] = VariableList[i].Generation;

		if (this.VariableList.Any(variable => !variable.Instantiated()))
		{
			result = ConstraintOperationResult.Undecided;
			return;
		}

		result = ConstraintOperationResult.Satisfied;
	}

	public void Propagate(out ConstraintOperationResult result)
	{
		if (this.Graph == null)
		{
			this.Graph = new BipartiteGraph(this.VariableList);

			if (this.Graph.MaximalMatching(this.lastMatching) < this.VariableList.Length)
			{
				result = ConstraintOperationResult.Violated;
				return;
			}
		}
		else if (!IncrementalUpdate())
		{
			result = ConstraintOperationResult.Violated;
			return;
		}

		SaveMatching();

		this.Graph.ResetSccState();
		this.Graph.BuildDirectedForScc();
		this.cycleDetection.Graph = this.Graph;
		this.cycleDetection.DetectCycle();

		result = ConstraintOperationResult.Undecided;
		foreach (var cycle in this.cycleDetection.StronglyConnectedComponents)
		{
			foreach (var node in cycle)
			{
				if (!(node is NodeVariable) || node == this.Graph.NullNode)
					continue;

				var variable = ((NodeVariable) node).Variable;
				foreach (var value in variable.Domain.Where(value =>
					this.Graph.Values[value].CycleIndex != node.CycleIndex &&
					((NodeValue) this.Graph.Pair[node]).Value != value))
				{
					variable.Remove(value, out DomainOperationResult domainResult);

					if (domainResult == DomainOperationResult.ElementNotInDomain)
						continue;

					result = ConstraintOperationResult.Propagated;

					if (domainResult == DomainOperationResult.EmptyDomain)
					{
						result = ConstraintOperationResult.Violated;
						return;
					}
				}
			}
		}
	}

	public void Explain(int variableId, bool isLowerBound, int boundValue,
		IReadOnlyList<int> snapshotLowerBounds, IReadOnlyList<int> snapshotUpperBounds, IList<BoundReason> result)
	{
		if (TryExplainHall(variableId, isLowerBound, boundValue, snapshotLowerBounds, snapshotUpperBounds, result))
			return;

		for (var i = 0; i < this.VariableList.Length; ++i)
		{
			result.Add(new BoundReason(this.VariableList[i].VariableId, true, snapshotLowerBounds[i]));
			result.Add(new BoundReason(this.VariableList[i].VariableId, false, snapshotUpperBounds[i]));
		}
	}

	private bool TryExplainHall(int variableId, bool isLowerBound, int boundValue,
		IReadOnlyList<int> snapshotLowerBounds, IReadOnlyList<int> snapshotUpperBounds, IList<BoundReason> result)
	{
		var target = -1;
		for (var i = 0; i < this.VariableList.Length; ++i)
		{
			if (this.VariableList[i].VariableId != variableId)
				continue;

			target = i;
			break;
		}

		if (target < 0)
			return false;

		var hall = isLowerBound
			? FindHallForLowerBound(target, boundValue, snapshotLowerBounds, snapshotUpperBounds)
			: FindHallForUpperBound(target, boundValue, snapshotLowerBounds, snapshotUpperBounds);

		if (hall == null)
			return false;

		foreach (var i in hall)
		{
			result.Add(new BoundReason(this.VariableList[i].VariableId, true, snapshotLowerBounds[i]));
			result.Add(new BoundReason(this.VariableList[i].VariableId, false, snapshotUpperBounds[i]));
		}

		result.Add(isLowerBound
			? new BoundReason(variableId, true, snapshotLowerBounds[target])
			: new BoundReason(variableId, false, snapshotUpperBounds[target]));

		return true;
	}

	private IList<int>? FindHallForLowerBound(int target, int boundValue,
		IReadOnlyList<int> snapshotLowerBounds, IReadOnlyList<int> snapshotUpperBounds)
	{
		var upper = boundValue - 1;
		var floor = snapshotLowerBounds[0];
		foreach (var lowerBound in snapshotLowerBounds)
			floor = Math.Min(floor, lowerBound);

		for (var lower = snapshotLowerBounds[target]; lower >= floor; --lower)
		{
			var members = CollectContained(target, lower, upper, snapshotLowerBounds, snapshotUpperBounds);
			if (members.Count == upper - lower + 1)
				return members;
		}

		return null;
	}

	private IList<int>? FindHallForUpperBound(int target, int boundValue,
		IReadOnlyList<int> snapshotLowerBounds, IReadOnlyList<int> snapshotUpperBounds)
	{
		var lower = boundValue + 1;
		var ceiling = snapshotUpperBounds[0];
		foreach (var upperBound in snapshotUpperBounds)
			ceiling = Math.Max(ceiling, upperBound);

		for (var upper = snapshotUpperBounds[target]; upper <= ceiling; ++upper)
		{
			var members = CollectContained(target, lower, upper, snapshotLowerBounds, snapshotUpperBounds);
			if (members.Count == upper - lower + 1)
				return members;
		}

		return null;
	}

	private IList<int> CollectContained(int target, int lower, int upper,
		IReadOnlyList<int> snapshotLowerBounds, IReadOnlyList<int> snapshotUpperBounds)
	{
		var members = new List<int>();
		for (var i = 0; i < this.VariableList.Length; ++i)
		{
			if (i == target)
				continue;

			if (snapshotLowerBounds[i] >= lower && snapshotUpperBounds[i] <= upper)
				members.Add(i);
		}

		return members;
	}

	private bool IncrementalUpdate()
	{
		var brokenVariables = new List<NodeVariable>();

		for (var i = 0; i < this.VariableList.Length; ++i)
		{
			var variable = this.VariableList[i];
			var varNode = this.Graph!.Variables[i];
			var matchBroken = false;
			var domainValues = new HashSet<int>(variable.Domain);

			foreach (var adjNode in new List<Node>(varNode.BipartiteEdges))
			{
				var valNode = (NodeValue) adjNode;
				if (domainValues.Contains(valNode.Value))
					continue;

				if (this.Graph.Pair[varNode] == valNode)
					matchBroken = true;

				this.Graph.RemoveEdge(varNode, valNode);
			}

			if (!matchBroken)
				continue;

			var matchedVal = this.Graph.Pair[varNode];
			this.Graph.Pair[varNode] = this.Graph.NullNode;
			this.Graph.Pair[matchedVal] = this.Graph.NullNode;
			brokenVariables.Add(varNode);
		}

		foreach (var varNode in brokenVariables)
		{
			if (!this.Graph!.RepairMatching(varNode))
				return false;
		}

		return true;
	}

	public bool StateChanged()
	{
		for (var i = 0; i < this.VariableList.Length; ++i)
		{
			if (this.VariableList[i].Generation != this.GenerationList[i])
				return true;
		}

		return false;
	}

	private void SaveMatching()
	{
		var matching = new int?[this.VariableList.Length];
		for (var i = 0; i < this.VariableList.Length; ++i)
		{
			var paired = this.Graph!.Pair[this.Graph.Variables[i]];
			if (paired != this.Graph.NullNode)
				matching[i] = ((NodeValue) paired).Value;
		}

		this.matchingTrail.Push((this.Depth, matching));
		this.lastMatching = matching;
	}

	public void OnBacktrack(int toDepth)
	{
		while (this.matchingTrail.Count > 0 && this.matchingTrail.Peek().Depth > toDepth)
			this.matchingTrail.Pop();

		this.lastMatching = this.matchingTrail.Count > 0
			? this.matchingTrail.Peek().Matching
			: null;

		this.Graph = null;
	}
}
