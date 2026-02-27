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
	private Dictionary<(int, bool), List<BoundReason>> PropagationExplanations { get; set; }

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
		this.PropagationExplanations = new();
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
		this.PropagationExplanations = new Dictionary<(int, bool), List<BoundReason>>();

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

		var sccBoundReasons = BuildSccBoundReasons();

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
					result = ConstraintOperationResult.Propagated;

					var oldLb = variable.Domain.LowerBound;
					var oldUb = variable.Domain.UpperBound;
					var valueSccIndex = this.Graph.Values[value].CycleIndex;

					variable.Remove(value, this.Depth, out DomainOperationResult domainResult);

					if (sccBoundReasons.TryGetValue(valueSccIndex, out var hallReasons))
					{
						if (variable.Domain.LowerBound > oldLb)
							MergeExplanation(variable.VariableId, true, hallReasons);
						if (variable.Domain.UpperBound < oldUb)
							MergeExplanation(variable.VariableId, false, hallReasons);
					}

					if (domainResult != DomainOperationResult.EmptyDomain)
						continue;

					result = ConstraintOperationResult.Violated;
					return;
				}
			}
		}
	}

	public void Explain(int variableId, bool isLowerBound, int boundValue, IList<BoundReason> result)
	{
		if (this.PropagationExplanations.TryGetValue((variableId, isLowerBound), out var reasons))
		{
			foreach (var reason in reasons)
				result.Add(reason);
			return;
		}

		foreach (var v in this.VariableList)
		{
			result.Add(new BoundReason(v.VariableId, true, v.Domain.LowerBound));
			result.Add(new BoundReason(v.VariableId, false, v.Domain.UpperBound));
		}
	}

	private Dictionary<int, List<BoundReason>> BuildSccBoundReasons()
	{
		var sccBoundReasons = new Dictionary<int, List<BoundReason>>();
		foreach (var kvp in this.Graph!.Variables)
		{
			var cycleIdx = kvp.Value.CycleIndex;
			var hallVar = kvp.Value.Variable;
			if (!sccBoundReasons.TryGetValue(cycleIdx, out var reasons))
			{
				reasons = new List<BoundReason>();
				sccBoundReasons[cycleIdx] = reasons;
			}
			reasons.Add(new BoundReason(hallVar.VariableId, true, hallVar.Domain.LowerBound));
			reasons.Add(new BoundReason(hallVar.VariableId, false, hallVar.Domain.UpperBound));
		}
		return sccBoundReasons;
	}

	private void MergeExplanation(int variableId, bool isLowerBound, List<BoundReason> reasons)
	{
		var key = (variableId, isLowerBound);
		if (!this.PropagationExplanations.TryGetValue(key, out var existing))
		{
			this.PropagationExplanations[key] = new List<BoundReason>(reasons);
			return;
		}
		foreach (var reason in reasons)
		{
			if (!existing.Contains(reason))
				existing.Add(reason);
		}
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
