/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System;
using System.Collections.Generic;

using Decider.Csp.BaseTypes;

namespace Decider.Csp.Integer;

public class MostConstrainedOrdering : IVariableOrderingHeuristic<int>
{
	public IVariable<int> SelectVariable(LinkedList<IVariable<int>> list)
	{
		var temp = list.First;
		var node = list.First;

		while (node != null)
		{
			if (node.Value.Size() < temp!.Value.Size())
				temp = node;

			if (temp.Value.Size() == 1)
				break;

			node = node.Next;
		}
		list.Remove(temp!);

		return temp!.Value;
	}
}

public class RandomOrdering : IVariableOrderingHeuristic<int>
{
	private readonly Random ran = new Random();

	public IVariable<int> SelectVariable(LinkedList<IVariable<int>> list)
	{
		var index = ran.Next(0, list.Count - 1);
		var node = list.First;
		while (--index >= 0)
			node = node!.Next;
		list.Remove(node!);
		return node!.Value;
	}
}

public class FirstVariableOrdering : IVariableOrderingHeuristic<int>
{
	public IVariable<int> SelectVariable(LinkedList<IVariable<int>> list)
	{
		var first = list.First;
		list.Remove(first!);
		return first!.Value;
	}
}

public class LastVariableOrdering : IVariableOrderingHeuristic<int>
{
	public IVariable<int> SelectVariable(LinkedList<IVariable<int>> list)
	{
		var last = list.Last;
		list.Remove(last!);
		return last!.Value;
	}
}
