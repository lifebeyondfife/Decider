/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using Decider.Csp.BaseTypes;

namespace Decider.Csp.Integer;

public class LowestValueOrdering : IValueOrderingHeuristic<int>
{
	public int SelectValue(IVariable<int> variable)
	{
		return ((VariableInteger) variable).Domain.LowerBound;
	}
}

public class MiddleValueOrdering : IValueOrderingHeuristic<int>
{
	public int SelectValue(IVariable<int> variable)
	{
		var domain = ((VariableInteger) variable).Domain;
		var target = domain.Size() / 2;
		var count = 0;

		foreach (var value in domain)
		{
			if (count == target)
				return value;

			++count;
		}

		return domain.LowerBound;
	}
}
