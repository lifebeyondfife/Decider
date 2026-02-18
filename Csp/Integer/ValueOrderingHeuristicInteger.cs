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
