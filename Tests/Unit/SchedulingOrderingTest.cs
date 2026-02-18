/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System.Collections.Generic;
using System.Linq;
using Xunit;

using Decider.Csp.BaseTypes;
using Decider.Csp.Global;
using Decider.Csp.Integer;

namespace Decider.Tests.Csp;

public class SchedulingOrderingTest
{
	[Fact]
	public void TestSelectsEarliestStartTimeFirst()
	{
		var s0 = new VariableInteger("s0", 5, 10);
		var s1 = new VariableInteger("s1", 0, 10);
		var s2 = new VariableInteger("s2", 3, 10);
		var starts = new List<VariableInteger> { s0, s1, s2 };
		var durations = new List<int> { 2, 2, 2 };
		var demands = new List<int> { 1, 1, 1 };

		var ordering = new SchedulingOrdering(starts, durations, demands, 3);
		var list = new LinkedList<IVariable<int>>(starts);

		var selected = ordering.SelectVariable(list);

		Assert.Same(s1, selected);
		Assert.Equal(2, list.Count);
	}

	[Fact]
	public void TestTiebreaksByLargestDemand()
	{
		var s0 = new VariableInteger("s0", 0, 10);
		var s1 = new VariableInteger("s1", 0, 10);
		var s2 = new VariableInteger("s2", 0, 10);
		var starts = new List<VariableInteger> { s0, s1, s2 };
		var durations = new List<int> { 2, 2, 2 };
		var demands = new List<int> { 2, 5, 3 };

		var ordering = new SchedulingOrdering(starts, durations, demands, 5);
		var list = new LinkedList<IVariable<int>>(starts);

		var selected = ordering.SelectVariable(list);

		Assert.Same(s1, selected);
	}

	[Fact]
	public void TestTiebreaksBySmallestDomain()
	{
		var s0 = new VariableInteger("s0", 0, 10);
		var s1 = new VariableInteger("s1", 0, 5);
		var s2 = new VariableInteger("s2", 0, 8);
		var starts = new List<VariableInteger> { s0, s1, s2 };
		var durations = new List<int> { 2, 2, 2 };
		var demands = new List<int> { 3, 3, 3 };

		var ordering = new SchedulingOrdering(starts, durations, demands, 5);
		var list = new LinkedList<IVariable<int>>(starts);

		var selected = ordering.SelectVariable(list);

		Assert.Same(s1, selected);
	}

	[Fact]
	public void TestSchedulingVariablesSelectedBeforeNonScheduling()
	{
		var s0 = new VariableInteger("s0", 5, 10);
		var makespan = new VariableInteger("makespan", 0, 100);
		var starts = new List<VariableInteger> { s0 };
		var durations = new List<int> { 2 };
		var demands = new List<int> { 3 };

		var ordering = new SchedulingOrdering(starts, durations, demands, 5);
		var list = new LinkedList<IVariable<int>>(new IVariable<int>[] { makespan, s0 });

		var selected = ordering.SelectVariable(list);

		Assert.Same(s0, selected);
	}

	[Fact]
	public void TestNonSchedulingFallsBackToMostConstrained()
	{
		var extra1 = new VariableInteger("extra1", 0, 100);
		var extra2 = new VariableInteger("extra2", 0, 5);
		var starts = new List<VariableInteger>();
		var durations = new List<int>();
		var demands = new List<int>();

		var ordering = new SchedulingOrdering(starts, durations, demands, 5);
		var list = new LinkedList<IVariable<int>>(new IVariable<int>[] { extra1, extra2 });

		var selected = ordering.SelectVariable(list);

		Assert.Same(extra2, selected);
	}

	[Fact]
	public void TestSequentialSelectionByEst()
	{
		var s0 = new VariableInteger("s0", 0, 10);
		var s1 = new VariableInteger("s1", 5, 10);
		var s2 = new VariableInteger("s2", 3, 10);
		var starts = new List<VariableInteger> { s0, s1, s2 };
		var durations = new List<int> { 2, 2, 2 };
		var demands = new List<int> { 1, 1, 1 };

		var ordering = new SchedulingOrdering(starts, durations, demands, 3);
		var list = new LinkedList<IVariable<int>>(starts);

		var first = ordering.SelectVariable(list);
		Assert.Same(s0, first);

		var second = ordering.SelectVariable(list);
		Assert.Same(s2, second);

		var third = ordering.SelectVariable(list);
		Assert.Same(s1, third);
	}

	[Fact]
	public void TestValueSelectionReturnsLowestForNonSchedulingVariable()
	{
		var starts = new List<VariableInteger>();
		var durations = new List<int>();
		var demands = new List<int>();
		var ordering = new SchedulingOrdering(starts, durations, demands, 5);

		var extra = new VariableInteger("extra", 3, 10);

		Assert.Equal(3, ordering.SelectValue(extra));
	}

	[Fact]
	public void TestValueSelectionReturnsLowestWhenNoConflict()
	{
		var s0 = new VariableInteger("s0", 0, 10);
		var starts = new List<VariableInteger> { s0 };
		var durations = new List<int> { 3 };
		var demands = new List<int> { 2 };

		var ordering = new SchedulingOrdering(starts, durations, demands, 5);

		Assert.Equal(0, ordering.SelectValue(s0));
	}

	[Fact]
	public void TestValueSelectionAvoidsResourceConflict()
	{
		var s0 = new VariableInteger("s0", 0, 10);
		var s1 = new VariableInteger("s1", 0, 10);
		var starts = new List<VariableInteger> { s0, s1 };
		var durations = new List<int> { 3, 2 };
		var demands = new List<int> { 3, 3 };

		var constraints = new List<IConstraint>
		{
			new CumulativeInteger(starts, durations, demands, 3)
		};

		var ordering = new SchedulingOrdering(starts, durations, demands, 3);
		var state = new StateInteger(starts, constraints, ordering, ordering);

		s0.Instantiate(0, 0, out _);

		var selectedValue = ordering.SelectValue(s1);

		Assert.True(selectedValue >= 3,
			$"Expected value >= 3 (after s0 finishes at t=3), got {selectedValue}");
	}

	[Fact]
	public void TestSchedulingOrderingFindsOptimalSolution()
	{
		var durations = new List<int> { 2, 3 };
		var demandValues = new List<int> { 3, 3 };

		var starts = new List<VariableInteger>
		{
			new VariableInteger("task_0", 0, 6),
			new VariableInteger("task_1", 0, 6)
		};

		var makespan = new VariableInteger("makespan", 0, 8);

		var constraints = new List<IConstraint>
		{
			new CumulativeInteger(starts, durations, demandValues, 3)
		};

		foreach (var i in Enumerable.Range(0, 2))
			constraints.Add(new ConstraintInteger(makespan >= starts[i] + durations[i]));

		var variables = new List<VariableInteger>(starts) { makespan };
		var ordering = new SchedulingOrdering(starts, durations, demandValues, 3);
		var state = new StateInteger(variables, constraints, ordering, ordering);
		var result = state.Search(makespan);

		Assert.Equal(StateOperationResult.Solved, result);
		Assert.Equal(5, state.OptimalSolution!["makespan"].InstantiatedValue);
	}
}
