/*
  Copyright © Iain McDonald 2010-2013
  
  This file is part of Decider.

	Decider is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	Decider is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with Decider.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Linq;

namespace Decider.Csp.Global
{
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
			nodeStack.Push(node);

			foreach (var adjoiningNode in node.AdjoiningNodes)
			{
				if (adjoiningNode.Index < 0)
				{
					Connection(adjoiningNode, ref cycles);
					node.Link = Math.Min(node.Link, adjoiningNode.Link);
				}
				else if (nodeStack.Contains(adjoiningNode))
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
				lastNode.CycleIndex = cycles;
				stronglyConnectedComponent.Add(lastNode);
			} while (node != lastNode);

			++cycles;
			this.StronglyConnectedComponents.Add(stronglyConnectedComponent);
		}
	}
}
