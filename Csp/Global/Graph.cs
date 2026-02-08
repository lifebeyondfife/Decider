/*
  Copyright © Iain McDonald 2010-2026
  
  This file is part of Decider.
*/
using System;
using System.Collections.Generic;
using System.Linq;

using Decider.Csp.Integer;

namespace Decider.Csp.Global;

internal class Graph
{
	public List<Node> Nodes { get; set; } = new List<Node>();
}

internal class BipartiteGraph : Graph
{
	internal Dictionary<int, NodeVariable> Variables { get; set; }
	internal Dictionary<int, NodeValue> Values { get; set; }
	internal Dictionary<Node, Node> Pair { get; private set; }

	public Node NullNode { get; private set; }
	private Dictionary<Node, int> Distance { get; set; } = new Dictionary<Node, int>();
	private Queue<Node> queue = new Queue<Node>();

	internal BipartiteGraph(IEnumerable<VariableInteger> variables)
	{
		this.Variables = new Dictionary<int, NodeVariable>();
		this.Values = new Dictionary<int, NodeValue>();
		var linkedList = new LinkedList<Node>();

		int index = 0;
		foreach (var variable in variables)
		{
			this.Variables[index] = new NodeVariable(variable);
			linkedList.AddLast(this.Variables[index]);
			foreach (int value in variable.Domain)
			{
				if (!this.Values.ContainsKey(value))
				{
					this.Values[value] = new NodeValue(value);
					linkedList.AddLast(this.Values[value]);
				}

				this.Variables[index].BipartiteEdges.AddLast(this.Values[value]);
				this.Values[value].BipartiteEdges.AddLast(this.Variables[index]);
			}

			++index;
		}

		this.Nodes = new List<Node>(linkedList);
		this.NullNode = new Node("NULL");
		this.Pair = new Dictionary<Node, Node>(this.Nodes.Count + 1);
		foreach (var node in this.Nodes)
			this.Pair[node] = this.NullNode;
	}

	internal int MaximalMatching(int?[]? seedMatching = null)
	{
		var matching = 0;
		this.queue = new Queue<Node>();
		this.Distance = new Dictionary<Node, int>(this.Nodes.Count + 1);

		foreach (var node in this.Nodes)
			this.Pair[node] = this.NullNode;

		if (seedMatching != null)
		{
			for (var i = 0; i < seedMatching.Length; ++i)
			{
				if (!seedMatching[i].HasValue)
					continue;

				var value = seedMatching[i]!.Value;
				if (!this.Variables.ContainsKey(i) || !this.Values.ContainsKey(value))
					continue;

				var varNode = this.Variables[i];
				var valNode = this.Values[value];

				if (!varNode.BipartiteEdges.Contains(valNode))
					continue;

				this.Pair[varNode] = valNode;
				this.Pair[valNode] = varNode;
				++matching;
			}
		}

		while (BreadthFirstSearch())
		{
			matching += this.Variables.Values.Count(node => this.Pair[node] == this.NullNode && DepthFirstSearch(node));
		}

		return matching;
	}

	internal void RemoveEdge(NodeVariable varNode, NodeValue valNode)
	{
		varNode.BipartiteEdges.Remove(valNode);
		valNode.BipartiteEdges.Remove(varNode);
	}

	internal bool RepairMatching(NodeVariable startNode)
	{
		var visited = new HashSet<Node>();
		return AugmentingPathDfs(startNode, visited);
	}

	private bool AugmentingPathDfs(Node varNode, HashSet<Node> visited)
	{
		foreach (var valNode in varNode.BipartiteEdges)
		{
			if (visited.Contains(valNode))
				continue;
			visited.Add(valNode);

			var matchedVar = this.Pair[valNode];
			if (matchedVar == this.NullNode || AugmentingPathDfs(matchedVar, visited))
			{
				this.Pair[varNode] = valNode;
				this.Pair[valNode] = varNode;
				return true;
			}
		}
		return false;
	}

	internal void BuildDirectedForScc()
	{
		this.NullNode.AdjoiningNodes = new LinkedList<Node>();

		var matchedValues = new HashSet<Node>();
		foreach (var varNode in this.Variables.Values)
		{
			var matchedVal = this.Pair[varNode];
			varNode.AdjoiningNodes = new LinkedList<Node>(new[] { matchedVal });

			if (matchedVal == this.NullNode)
				continue;

			matchedValues.Add(matchedVal);
			var adjacency = new LinkedList<Node>();
			adjacency.AddLast(this.NullNode);
			foreach (var neighbor in matchedVal.BipartiteEdges)
			{
				if (neighbor != varNode)
					adjacency.AddLast(neighbor);
			}
			matchedVal.AdjoiningNodes = adjacency;
		}

		foreach (var valNode in this.Values.Values)
		{
			if (matchedValues.Contains(valNode))
				continue;
			this.NullNode.AdjoiningNodes.AddLast(valNode);
			valNode.AdjoiningNodes = new LinkedList<Node>(valNode.BipartiteEdges);
		}
	}

	internal void ResetSccState()
	{
		foreach (var node in this.Nodes)
		{
			node.Index = -1;
			node.Link = -1;
			node.CycleIndex = -1;
			node.OnStack = false;
		}
		this.NullNode.Index = -1;
		this.NullNode.Link = -1;
		this.NullNode.CycleIndex = -1;
		this.NullNode.OnStack = false;
	}

	private bool BreadthFirstSearch()
	{
		foreach (var node in this.Variables.Values)
			if (this.Pair[node] == this.NullNode)
			{
				this.Distance[node] = 0;
				this.queue.Enqueue(node);
			}
			else
				this.Distance[node] = Int32.MaxValue;

		this.Distance[this.NullNode] = Int32.MaxValue;

		while (this.queue.Any())
		{
			var node = this.queue.Dequeue();
			foreach (var adjoinedNode in node.BipartiteEdges.
				Where(adjoinedNode => this.Distance[this.Pair[adjoinedNode]] == Int32.MaxValue))
			{
				this.Distance[this.Pair[adjoinedNode]] = this.Distance[node] + 1;
				this.queue.Enqueue(this.Pair[adjoinedNode]);
			}
		}

		return this.Distance[this.NullNode] != Int32.MaxValue;
	}

	private bool DepthFirstSearch(Node node)
	{
		if (node == this.NullNode)
			return true;

		foreach (var adjoinedNode in node.BipartiteEdges.Where(adjoinedNode =>
			(this.Distance[this.Pair[adjoinedNode]] == this.Distance[node] + 1) &&
			DepthFirstSearch(this.Pair[adjoinedNode])))
		{
			this.Pair[adjoinedNode] = node;
			this.Pair[node] = adjoinedNode;
			return true;
		}

		this.Distance[node] = Int32.MaxValue;
		return false;
	}
}
