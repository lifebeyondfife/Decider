/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System.Collections.Generic;
using Xunit;

using Decider.Csp.BaseTypes;
using Decider.Csp.Integer;

namespace Decider.Tests.Csp;

public class OrderingHeuristicTest
{
	[Fact]
	public void DomWdeg_NoWeights_FallsBackToSmallestDomain()
	{
		var v0 = new VariableInteger("v0", 0, 9);
		var v1 = new VariableInteger("v1", 0, 4);
		var v2 = new VariableInteger("v2", 0, 14);
		var variables = new List<IVariable<int>> { v0, v1, v2 };
		var ordering = new DomWdegOrdering(variables, []);

		var list = new List<IVariable<int>>(variables);
		var selected = list[ordering.SelectVariableIndex(list)];

		Assert.Same(v1, selected);
	}

	[Fact]
	public void DomWdeg_WithWeights_SelectsLowestRatio()
	{
		var v0 = new VariableInteger("v0", 0, 4);
		var v1 = new VariableInteger("v1", 0, 9);
		var variables = new List<IVariable<int>> { v0, v1 };

		var constraint = new ConstraintInteger(v1 > 0);
		constraint.FailureWeight = 10;
		var ordering = new DomWdegOrdering(variables, [constraint]);

		var list = new List<IVariable<int>>(variables);
		var selected = list[ordering.SelectVariableIndex(list)];

		Assert.Same(v1, selected);
	}

	[Fact]
	public void DomWdeg_SolvesNQueens4_WithCorrectSolutionCount()
	{
		var queens = new List<VariableInteger>
		{
			new("0", 0, 3),
			new("1", 0, 3),
			new("2", 0, 3),
			new("3", 0, 3)
		};

		var constraints = new List<IConstraint>();
		for (var i = 0; i < queens.Count; ++i)
		{
			for (var j = i + 1; j < queens.Count; ++j)
			{
				constraints.Add(new ConstraintInteger(queens[i] != queens[j]));
				constraints.Add(new ConstraintInteger(queens[i] - queens[j] != i - j));
				constraints.Add(new ConstraintInteger(queens[j] - queens[i] != i - j));
			}
		}

		var variables = new List<IVariable<int>>(queens);
		var state = new StateInteger(queens, constraints, new DomWdegOrdering(variables, constraints));
		state.SearchAllSolutions();

		Assert.Equal(2, state.Solutions.Count);
	}

	[Fact]
	public void MiddleValueOrdering_ContinuousDomain_ReturnsMidpoint()
	{
		var v = new VariableInteger("v", 0, 9);
		var ordering = new MiddleValueOrdering();

		Assert.Equal(5, ordering.SelectValue(v));
	}

	[Fact]
	public void MiddleValueOrdering_SingleElement_ReturnsThatElement()
	{
		var v = new VariableInteger("v", 7, 7);
		var ordering = new MiddleValueOrdering();

		Assert.Equal(7, ordering.SelectValue(v));
	}

	[Fact]
	public void MiddleValueOrdering_TwoElements_ReturnsUpper()
	{
		var v = new VariableInteger("v", 3, 4);
		var ordering = new MiddleValueOrdering();

		Assert.Equal(4, ordering.SelectValue(v));
	}

	[Fact]
	public void MiddleValueOrdering_NegativeDomain_ReturnsMidpoint()
	{
		var v = new VariableInteger("v", -4, 4);
		var ordering = new MiddleValueOrdering();

		Assert.Equal(0, ordering.SelectValue(v));
	}
}
