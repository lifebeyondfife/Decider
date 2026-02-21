/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System.Collections.Generic;

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

public class SolutionGuidedValueOrdering : IValueOrderingHeuristic<int>
{
	private readonly IValueOrderingHeuristic<int> baseOrdering;
	private readonly Dictionary<int, int> preferredValues;

	public SolutionGuidedValueOrdering(IValueOrderingHeuristic<int> baseOrdering)
	{
		this.baseOrdering = baseOrdering;
		this.preferredValues = new Dictionary<int, int>();
	}

	public void UpdatePreferredValues(IVariable<int>[] solution)
	{
		this.preferredValues.Clear();
		foreach (var variable in solution)
			this.preferredValues[variable.VariableId] = variable.InstantiatedValue;
	}

	public int SelectValue(IVariable<int> variable)
	{
		if (this.preferredValues.TryGetValue(variable.VariableId, out var preferred) &&
			((VariableInteger) variable).Domain.Contains(preferred))
			return preferred;

		return this.baseOrdering.SelectValue(variable);
	}
}
