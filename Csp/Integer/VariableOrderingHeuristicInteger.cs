/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System;
using System.Collections.Generic;
using System.Linq;
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

public class DomWdegOrdering : IVariableOrderingHeuristic<int>
{
	private Dictionary<IVariable<int>, IList<IConstraint>> VariableConstraints { get; }

	public DomWdegOrdering(IEnumerable<IVariable<int>> variables, IList<IConstraint> constraints)
	{
		var variablesList = variables.ToList();
		this.VariableConstraints = new Dictionary<IVariable<int>, IList<IConstraint>>(variablesList.Count);
		foreach (var variable in variables)
			this.VariableConstraints[variable] = new List<IConstraint>();

		foreach (var constraint in constraints)
		{
			if (constraint is not IConstraint<int> typed)
				continue;

			foreach (var variable in typed.Variables)
			{
				if (this.VariableConstraints.TryGetValue(variable, out var list))
					list.Add(constraint);
			}
		}
	}

	public int SelectVariableIndex(IList<IVariable<int>> variables)
	{
		var bestIndex = 0;
		var bestScore = double.MaxValue;

		for (var i = 0; i < variables.Count; ++i)
		{
			var score = ComputeScore(variables[i]);
			if (score < bestScore)
			{
				bestScore = score;
				bestIndex = i;
			}
		}

		return bestIndex;
	}

	private double ComputeScore(IVariable<int> variable)
	{
		var totalWeight = 0;

		if (this.VariableConstraints.TryGetValue(variable, out var constraints))
			foreach (var constraint in constraints)
				totalWeight += constraint.FailureWeight;

		return totalWeight == 0 ? variable.Size() : (double) variable.Size() / totalWeight;
	}
}
