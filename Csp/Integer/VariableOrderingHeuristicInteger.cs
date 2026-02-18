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
	public int SelectVariableIndex(IList<IVariable<int>> variables)
	{
		var bestIndex = 0;
		for (var i = 1; i < variables.Count; ++i)
		{
			if (variables[i].Size() < variables[bestIndex].Size())
				bestIndex = i;

			if (variables[bestIndex].Size() == 1)
				break;
		}

		return bestIndex;
	}
}

public class RandomOrdering : IVariableOrderingHeuristic<int>
{
	private readonly Random ran = new Random();

	public int SelectVariableIndex(IList<IVariable<int>> variables)
	{
		return ran.Next(0, variables.Count);
	}
}

public class FirstVariableOrdering : IVariableOrderingHeuristic<int>
{
	public int SelectVariableIndex(IList<IVariable<int>> variables)
	{
		return 0;
	}
}

public class LastVariableOrdering : IVariableOrderingHeuristic<int>
{
	public int SelectVariableIndex(IList<IVariable<int>> variables)
	{
		return variables.Count - 1;
	}
}
