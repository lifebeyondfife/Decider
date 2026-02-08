/*
  Copyright © Iain McDonald 2010-2026
  
  This file is part of Decider.
*/
using System;
using System.Collections.Generic;
using System.Linq;

namespace Decider.Csp.Global;

internal class CycleDetection
{
	private Stack<Node> nodeStack;
	private int index;

	internal List<List<Node>> StronglyConnectedComponents { get; set; }
	internal Graph Graph { get; set; }

	internal void DetectCycle()
	{
		this.StronglyConnectedComponents = new List<List<Node>>();
		this.index = 0;
		this.nodeStack = new Stack<Node>();

		var cycles = 0;
		foreach (var node in this.Graph.Nodes.Where(node => node.Index < 0))
			Connection(node, ref cycles);
	}

	private void Connection(Node node, ref int cycles)
	{
		node.Index = index;
		node.Link = index;
		index++;
		node.OnStack = true;
		nodeStack.Push(node);

		foreach (var adjoiningNode in node.AdjoiningNodes)
		{
			if (adjoiningNode.Index < 0)
			{
				Connection(adjoiningNode, ref cycles);
				node.Link = Math.Min(node.Link, adjoiningNode.Link);
			}
			else if (adjoiningNode.OnStack)
			{
				node.Link = Math.Min(node.Link, adjoiningNode.Index);
			}
		}

		if (node.Link != node.Index)
			return;

		var stronglyConnectedComponent = new List<Node>();
		Node lastNode;
		do
		{
			lastNode = nodeStack.Pop();
			lastNode.OnStack = false;
			lastNode.CycleIndex = cycles;
			stronglyConnectedComponent.Add(lastNode);
		} while (node != lastNode);

		++cycles;
		this.StronglyConnectedComponents.Add(stronglyConnectedComponent);
	}
}
