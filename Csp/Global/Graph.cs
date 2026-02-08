/*
  Copyright © Iain McDonald 2010-2022
  
  This file is part of Decider.
*/
using System;
using System.Collections.Generic;
using System.Linq;

using Decider.Csp.Integer;

namespace Decider.Csp.Global
{
	internal class Graph
	{
		public List<Node> Nodes { get; set; }
	}

	internal class BipartiteGraph : Graph
	{
		internal Dictionary<int, NodeVariable> Variables { get; set; }
		internal Dictionary<int, NodeValue> Values { get; set; }
		internal Dictionary<Node, int> Distance { get; set; }
		internal Dictionary<Node, Node> Pair { get; set; }

		public Node NullNode { get; private set; }
		private Queue<Node> queue;

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

					this.Variables[index].AdjoiningNodes.AddLast(this.Values[value]);
					this.Values[value].AdjoiningNodes.AddLast(this.Variables[index]);
				}

				++index;
			}

			this.Nodes = new List<Node>(linkedList);
		}

		internal int MaximalMatching(int?[] seedMatching = null)
		{
			var matching = 0;
			this.NullNode = new Node("NULL");
			this.queue = new Queue<Node>();
			this.Pair = new Dictionary<Node, Node>(this.Nodes.Count);
			this.Distance = new Dictionary<Node, int>(this.Nodes.Count);
			foreach (var node in this.Nodes)
				this.Pair[node] = this.NullNode;

			if (seedMatching != null)
			{
				for (var i = 0; i < seedMatching.Length; ++i)
				{
					if (!seedMatching[i].HasValue)
						continue;

					var value = seedMatching[i].Value;
					if (!this.Variables.ContainsKey(i) || !this.Values.ContainsKey(value))
						continue;

					var varNode = this.Variables[i];
					var valNode = this.Values[value];

					if (!varNode.AdjoiningNodes.Contains(valNode))
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

			UndirectedToDirected();

			return matching;
		}

		private void UndirectedToDirected()
		{
			var toNullNode = new LinkedList<Node>();
			foreach (var node in this.Variables.Values)
			{
				node.AdjoiningNodes = new LinkedList<Node>(new[] { this.Pair[node] });
				this.Pair[node].AdjoiningNodes.Remove(node);
				this.Pair[node].AdjoiningNodes.AddFirst(this.NullNode);
				toNullNode.AddLast(this.Pair[node]);
			}

			foreach (var node in this.Values.Values.Except(toNullNode))
			{
				this.NullNode.AdjoiningNodes.AddLast(node);
			}
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
				foreach (var adjoinedNode in node.AdjoiningNodes.
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

			foreach (var adjoinedNode in node.AdjoiningNodes.Where(adjoinedNode =>
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
}
