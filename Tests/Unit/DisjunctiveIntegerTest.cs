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

public class DisjunctiveIntegerTest
{
	[Fact]
	public void TestBasicSatisfaction()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 0, 10),
			new("s1", 0, 10)
		};
		var durations = new List<int> { 3, 2 };

		var constraints = new List<IConstraint>
		{
			new DisjunctiveInteger(starts, durations)
		};

		var state = new StateInteger(starts, constraints);
		var result = state.Search();

		Assert.Equal(StateOperationResult.Solved, result);
		Assert.Single(state.Solutions);
	}

	[Fact]
	public void TestViolationDetection()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 0, 0),
			new("s1", 0, 0)
		};
		var durations = new List<int> { 5, 5 };

		var constraints = new List<IConstraint>
		{
			new DisjunctiveInteger(starts, durations)
		};

		var state = new StateInteger(starts, constraints);
		var result = state.Search();

		Assert.Equal(StateOperationResult.Unsatisfiable, result);
	}

	[Fact]
	public void TestDetectablePrecedenceLowerBound()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 0, 0),
			new("s1", 0, 5)
		};
		var durations = new List<int> { 3, 2 };

		var constraints = new List<IConstraint>
		{
			new DisjunctiveInteger(starts, durations)
		};
		_ = new StateInteger(starts, constraints);

		constraints[0].Propagate(out var propagateResult);

		Assert.Equal(ConstraintOperationResult.Propagated, propagateResult);
		Assert.True(starts[1].Domain.LowerBound >= 3);
	}

	[Fact]
	public void TestDetectablePrecedenceUpperBound()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 0, 5),
			new("s1", 5, 5)
		};
		var durations = new List<int> { 3, 2 };

		var constraints = new List<IConstraint>
		{
			new DisjunctiveInteger(starts, durations)
		};
		_ = new StateInteger(starts, constraints);

		constraints[0].Propagate(out var propagateResult);

		Assert.Equal(ConstraintOperationResult.Propagated, propagateResult);
		Assert.True(starts[0].Domain.UpperBound <= 2);
	}

	[Fact]
	public void TestThreeTasksSerialized()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 0, 0),
			new("s1", 2, 2),
			new("s2", 0, 10)
		};
		var durations = new List<int> { 2, 2, 2 };

		var constraints = new List<IConstraint>
		{
			new DisjunctiveInteger(starts, durations)
		};
		_ = new StateInteger(starts, constraints);

		constraints[0].Propagate(out var propagateResult);

		Assert.Equal(ConstraintOperationResult.Propagated, propagateResult);
		Assert.True(starts[2].Domain.LowerBound >= 4);
	}

	[Fact]
	public void TestThreeTasksInfeasible()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 0, 1),
			new("s1", 0, 1),
			new("s2", 0, 1)
		};
		var durations = new List<int> { 2, 2, 2 };

		var constraints = new List<IConstraint>
		{
			new DisjunctiveInteger(starts, durations)
		};
		_ = new StateInteger(starts, constraints);

		constraints[0].Propagate(out var propagateResult);

		Assert.Equal(ConstraintOperationResult.Violated, propagateResult);
	}

	[Fact]
	public void TestNotFirstRuleBoundTightening()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 0, 1),
			new("s1", 0, 3),
			new("s2", 0, 10)
		};
		var durations = new List<int> { 2, 2, 3 };

		var constraints = new List<IConstraint>
		{
			new DisjunctiveInteger(starts, durations)
		};
		_ = new StateInteger(starts, constraints);

		constraints[0].Propagate(out var propagateResult);

		Assert.Equal(ConstraintOperationResult.Propagated, propagateResult);
		Assert.True(starts[2].Domain.LowerBound >= 4);
	}

	[Fact]
	public void TestEdgeFindingUpperBoundTightening()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 5, 5),
			new("s1", 7, 7),
			new("s2", 0, 6)
		};
		var durations = new List<int> { 2, 2, 2 };

		var constraints = new List<IConstraint>
		{
			new DisjunctiveInteger(starts, durations)
		};
		_ = new StateInteger(starts, constraints);

		constraints[0].Propagate(out var propagateResult);

		Assert.Equal(ConstraintOperationResult.Propagated, propagateResult);
		Assert.True(starts[2].Domain.UpperBound <= 3);
	}

	[Fact]
	public void TestCheckSatisfiedWhenNoOverlap()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 0, 0),
			new("s1", 3, 3)
		};
		var durations = new List<int> { 3, 2 };

		var constraints = new List<IConstraint>
		{
			new DisjunctiveInteger(starts, durations)
		};
		_ = new StateInteger(starts, constraints);

		constraints[0].Check(out var checkResult);

		Assert.Equal(ConstraintOperationResult.Satisfied, checkResult);
	}

	[Fact]
	public void TestCheckViolatedWhenOverlap()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 0, 0),
			new("s1", 2, 2)
		};
		var durations = new List<int> { 3, 2 };

		var constraints = new List<IConstraint>
		{
			new DisjunctiveInteger(starts, durations)
		};
		_ = new StateInteger(starts, constraints);

		constraints[0].Check(out var checkResult);

		Assert.Equal(ConstraintOperationResult.Violated, checkResult);
	}

	[Fact]
	public void TestMultipleSolutions()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 0, 4),
			new("s1", 0, 4)
		};
		var durations = new List<int> { 2, 2 };

		var constraints = new List<IConstraint>
		{
			new DisjunctiveInteger(starts, durations)
		};

		var state = new StateInteger(starts, constraints);
		var result = state.SearchAllSolutions();

		Assert.Equal(StateOperationResult.Solved, result);
		Assert.True(state.Solutions.Count >= 2);
	}

	[Fact]
	public void TestSingleTask()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 0, 10)
		};
		var durations = new List<int> { 5 };

		var constraints = new List<IConstraint>
		{
			new DisjunctiveInteger(starts, durations)
		};

		var state = new StateInteger(starts, constraints);
		var result = state.Search();

		Assert.Equal(StateOperationResult.Solved, result);
	}

	[Fact]
	public void TestFourTasksSearchFinds()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 0, 10),
			new("s1", 0, 10),
			new("s2", 0, 10),
			new("s3", 0, 10)
		};
		var durations = new List<int> { 2, 3, 2, 3 };

		var constraints = new List<IConstraint>
		{
			new DisjunctiveInteger(starts, durations)
		};

		var state = new StateInteger(starts, constraints);
		var result = state.Search();

		Assert.Equal(StateOperationResult.Solved, result);
		Assert.Single(state.Solutions);

		var solution = state.Solutions[0];
		foreach (var s0 in solution.Values)
		{
			foreach (var s1 in solution.Values)
			{
				if (s0.Name == s1.Name)
					continue;

				var idx0 = int.Parse(s0.Name.Substring(1));
				var idx1 = int.Parse(s1.Name.Substring(1));
				var end0 = s0.InstantiatedValue + durations[idx0];
				var start1 = s1.InstantiatedValue;

				Assert.True(end0 <= start1 || s1.InstantiatedValue + durations[idx1] <= s0.InstantiatedValue);
			}
		}
	}
}
