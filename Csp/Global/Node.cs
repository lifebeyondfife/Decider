/*
  Copyright © Iain McDonald 2010-2021
  
  This file is part of Decider.
*/
using System.Collections.Generic;

using Decider.Csp.Integer;

namespace Decider.Csp.Global
{
	internal class Node
	{
		internal int Index { get; set; }
		internal int Link { get; set; }
		internal LinkedList<Node> AdjoiningNodes { get; set; }
		internal string Label { get; private set; }
		internal int CycleIndex { get; set; }

		internal Node()
		{
			this.AdjoiningNodes = new LinkedList<Node>();
			this.Index = -1;
			this.Link = -1;
			this.CycleIndex = -1;
		}

		internal Node(string label)
			: this()
		{
			this.Label = label;
		}

		public override string ToString()
		{
			return this.Label;
		}
	}

	internal class NodeVariable : Node
	{
		internal VariableInteger Variable { get; set; }

		internal NodeVariable(VariableInteger variable, string label)
			: base(label)
		{
			this.Variable = variable;
		}

		internal NodeVariable(VariableInteger variable)
		{
			this.Variable = variable;
		}

	}

	internal class NodeValue : Node
	{
		internal int Value { get; set; }

		internal NodeValue(int value, string label)
			: base(label)
		{
			this.Value = value;
		}

		internal NodeValue(int value)
		{
			this.Value = value;
		}
	}
}
