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
