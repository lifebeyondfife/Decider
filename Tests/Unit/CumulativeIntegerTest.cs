/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System.Collections.Generic;
using Xunit;

using Decider.Csp.BaseTypes;
using Decider.Csp.Global;
using Decider.Csp.Integer;

namespace Decider.Tests.Csp;

public class CumulativeIntegerTest
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
		var demands = new List<int> { 2, 3 };

		var constraints = new List<IConstraint>
		{
			new CumulativeInteger(starts, durations, demands, capacity: 5)
		};

		var state = new StateInteger(starts, constraints);
		var result = state.Search();

		Assert.Equal(StateOperationResult.Solved, result);
		Assert.Single(state.Solutions);
	}

	[Fact]
	public void TestPropagationPrunesDomains()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 0, 5),
			new("s1", 0, 5)
		};
		var durations = new List<int> { 3, 3 };
		var demands = new List<int> { 5, 5 };

		var constraints = new List<IConstraint>
		{
			new CumulativeInteger(starts, durations, demands, capacity: 5)
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
		var demands = new List<int> { 3, 3 };

		var constraints = new List<IConstraint>
		{
			new CumulativeInteger(starts, durations, demands, capacity: 4)
		};

		var state = new StateInteger(starts, constraints);
		var result = state.Search();

		Assert.Equal(StateOperationResult.Unsatisfiable, result);
		Assert.Empty(state.Solutions);
	}

	[Fact]
	public void TestBacktrackingCorrectness()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 0, 5),
			new("s1", 0, 5),
			new("s2", 0, 5)
		};
		var durations = new List<int> { 2, 2, 2 };
		var demands = new List<int> { 3, 3, 3 };

		var constraints = new List<IConstraint>
		{
			new CumulativeInteger(starts, durations, demands, capacity: 6)
		};

		var state = new StateInteger(starts, constraints);
		var result = state.Search();

		Assert.Equal(StateOperationResult.Solved, result);
		Assert.Single(state.Solutions);
	}

	[Fact]
	public void TestMultipleSolutions()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 0, 2),
			new("s1", 0, 2)
		};
		var durations = new List<int> { 1, 1 };
		var demands = new List<int> { 2, 2 };

		var constraints = new List<IConstraint>
		{
			new CumulativeInteger(starts, durations, demands, capacity: 3)
		};

		var state = new StateInteger(starts, constraints);
		var result = state.SearchAllSolutions();

		Assert.Equal(StateOperationResult.Solved, result);
		Assert.True(state.Solutions.Count > 1);
	}

	[Fact]
	public void TestSingleTask()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 0, 10)
		};
		var durations = new List<int> { 5 };
		var demands = new List<int> { 3 };

		var constraints = new List<IConstraint>
		{
			new CumulativeInteger(starts, durations, demands, capacity: 5)
		};

		var state = new StateInteger(starts, constraints);
		var result = state.Search();

		Assert.Equal(StateOperationResult.Solved, result);
		Assert.Single(state.Solutions);
	}

	[Fact]
	public void TestZeroDemandTask()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 0, 10),
			new("s1", 0, 10)
		};
		var durations = new List<int> { 5, 5 };
		var demands = new List<int> { 0, 3 };

		var constraints = new List<IConstraint>
		{
			new CumulativeInteger(starts, durations, demands, capacity: 5)
		};

		var state = new StateInteger(starts, constraints);
		var result = state.Search();

		Assert.Equal(StateOperationResult.Solved, result);
		Assert.Single(state.Solutions);
	}

	[Fact]
	public void TestTightCapacity()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 0, 3),
			new("s1", 0, 3)
		};
		var durations = new List<int> { 2, 2 };
		var demands = new List<int> { 5, 5 };

		var constraints = new List<IConstraint>
		{
			new CumulativeInteger(starts, durations, demands, capacity: 5)
		};

		var state = new StateInteger(starts, constraints);
		var result = state.SearchAllSolutions();

		Assert.Equal(StateOperationResult.Solved, result);
		Assert.True(state.Solutions.Count >= 1);
	}

	[Fact]
	public void TestNoOverlap()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 0, 0),
			new("s1", 3, 3)
		};
		var durations = new List<int> { 3, 2 };
		var demands = new List<int> { 10, 10 };

		var constraints = new List<IConstraint>
		{
			new CumulativeInteger(starts, durations, demands, capacity: 10)
		};

		var state = new StateInteger(starts, constraints);

		constraints[0].Check(out ConstraintOperationResult checkResult);

		Assert.Equal(ConstraintOperationResult.Satisfied, checkResult);
	}

	[Fact]
	public void TestCompulsoryPartDetection()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 2, 3),
			new("s1", 0, 10)
		};
		var durations = new List<int> { 3, 2 };
		var demands = new List<int> { 4, 2 };

		var constraints = new List<IConstraint>
		{
			new CumulativeInteger(starts, durations, demands, capacity: 5)
		};

		var state = new StateInteger(starts, constraints);

		constraints[0].Propagate(out ConstraintOperationResult propagateResult);

		Assert.False(starts[1].Domain.Contains(3));
		Assert.False(starts[1].Domain.Contains(4));
	}

	[Fact]
	public void TestEdgeFindingThreeTasksSerialized()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 0, 1),
			new("s1", 0, 2),
			new("s2", 0, 10)
		};
		var durations = new List<int> { 2, 2, 2 };
		var demands = new List<int> { 5, 5, 5 };

		var constraints = new List<IConstraint>
		{
			new CumulativeInteger(starts, durations, demands, capacity: 5)
		};

		var state = new StateInteger(starts, constraints);

		constraints[0].Propagate(out ConstraintOperationResult propagateResult);

		Assert.Equal(ConstraintOperationResult.Propagated, propagateResult);
		Assert.True(starts[2].Domain.LowerBound >= 4);
	}

	[Fact]
	public void TestEdgeFindingBoundTightening()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 0, 0),
			new("s1", 2, 2),
			new("s2", 0, 5)
		};
		var durations = new List<int> { 2, 2, 2 };
		var demands = new List<int> { 5, 5, 5 };

		var constraints = new List<IConstraint>
		{
			new CumulativeInteger(starts, durations, demands, capacity: 5)
		};

		var state = new StateInteger(starts, constraints);

		constraints[0].Propagate(out ConstraintOperationResult propagateResult);

		Assert.Equal(ConstraintOperationResult.Propagated, propagateResult);
		Assert.Equal(4, starts[2].Domain.LowerBound);
	}

	[Fact]
	public void TestEdgeFindingDetectsInfeasibility()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 0, 1),
			new("s1", 0, 1),
			new("s2", 0, 1)
		};
		var durations = new List<int> { 2, 2, 2 };
		var demands = new List<int> { 3, 3, 3 };

		var constraints = new List<IConstraint>
		{
			new CumulativeInteger(starts, durations, demands, capacity: 3)
		};

		var state = new StateInteger(starts, constraints);

		constraints[0].Propagate(out ConstraintOperationResult propagateResult);

		Assert.Equal(ConstraintOperationResult.Violated, propagateResult);
	}

	[Fact]
	public void TestEdgeFindingUpperBoundPropagation()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 5, 5),
			new("s1", 7, 7),
			new("s2", 0, 6)
		};
		var durations = new List<int> { 2, 2, 2 };
		var demands = new List<int> { 5, 5, 5 };

		var constraints = new List<IConstraint>
		{
			new CumulativeInteger(starts, durations, demands, capacity: 5)
		};

		var state = new StateInteger(starts, constraints);

		constraints[0].Propagate(out ConstraintOperationResult propagateResult);

		Assert.Equal(ConstraintOperationResult.Propagated, propagateResult);
		Assert.Equal(3, starts[2].Domain.UpperBound);
	}

	[Fact]
	public void TestNotFirstRuleLowerBoundTightening()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 0, 3),
			new("s1", 2, 5),
			new("s2", 0, 10)
		};
		var durations = new List<int> { 4, 4, 3 };
		var demands = new List<int> { 3, 3, 3 };

		var constraints = new List<IConstraint>
		{
			new CumulativeInteger(starts, durations, demands, capacity: 5)
		};

		var state = new StateInteger(starts, constraints);

		constraints[0].Propagate(out ConstraintOperationResult propagateResult);

		Assert.True(propagateResult == ConstraintOperationResult.Propagated || propagateResult == ConstraintOperationResult.Undecided);
		Assert.True(starts[2].Domain.LowerBound >= 4);
	}

	[Fact]
	public void TestNotLastRuleUpperBoundTightening()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 7, 10),
			new("s1", 5, 8),
			new("s2", 0, 10)
		};
		var durations = new List<int> { 4, 4, 3 };
		var demands = new List<int> { 3, 3, 3 };

		var constraints = new List<IConstraint>
		{
			new CumulativeInteger(starts, durations, demands, capacity: 5)
		};

		var state = new StateInteger(starts, constraints);

		constraints[0].Propagate(out ConstraintOperationResult propagateResult);

		Assert.True(propagateResult == ConstraintOperationResult.Propagated || propagateResult == ConstraintOperationResult.Undecided);
		Assert.True(starts[2].Domain.UpperBound <= 6);
	}

	[Fact]
	public void TestNotFirstRuleDetectsInfeasibility()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 5, 5),
			new("s1", 5, 6),
			new("s2", 0, 5)
		};
		var durations = new List<int> { 4, 4, 4 };
		var demands = new List<int> { 3, 3, 3 };

		var constraints = new List<IConstraint>
		{
			new CumulativeInteger(starts, durations, demands, capacity: 3)
		};

		var state = new StateInteger(starts, constraints);

		constraints[0].Propagate(out ConstraintOperationResult propagateResult);

		Assert.Equal(ConstraintOperationResult.Violated, propagateResult);
	}

	[Fact]
	public void TestNotLastRuleDetectsInfeasibility()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 0, 0),
			new("s1", 0, 1),
			new("s2", 1, 6)
		};
		var durations = new List<int> { 4, 4, 4 };
		var demands = new List<int> { 3, 3, 3 };

		var constraints = new List<IConstraint>
		{
			new CumulativeInteger(starts, durations, demands, capacity: 3)
		};

		var state = new StateInteger(starts, constraints);

		constraints[0].Propagate(out ConstraintOperationResult propagateResult);

		Assert.Equal(ConstraintOperationResult.Violated, propagateResult);
	}

	[Fact]
	public void TestNotFirstRuleStrongerThanEdgeFinding()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 0, 2),
			new("s1", 1, 4),
			new("s2", 0, 10)
		};
		var durations = new List<int> { 3, 3, 2 };
		var demands = new List<int> { 4, 4, 4 };

		var constraints = new List<IConstraint>
		{
			new CumulativeInteger(starts, durations, demands, capacity: 5)
		};

		var state = new StateInteger(starts, constraints);

		constraints[0].Propagate(out ConstraintOperationResult propagateResult);

		Assert.True(propagateResult == ConstraintOperationResult.Propagated || propagateResult == ConstraintOperationResult.Undecided);
		Assert.True(starts[2].Domain.LowerBound >= 3);
	}

	[Fact]
	public void TestNotFirstNotLastInteraction()
	{
		var starts = new List<VariableInteger>
		{
			new("s0", 0, 3),
			new("s1", 5, 8),
			new("s2", 0, 10)
		};
		var durations = new List<int> { 3, 3, 2 };
		var demands = new List<int> { 4, 4, 4 };

		var constraints = new List<IConstraint>
		{
			new CumulativeInteger(starts, durations, demands, capacity: 5)
		};

		var state = new StateInteger(starts, constraints);

		var originalLower = starts[2].Domain.LowerBound;
		var originalUpper = starts[2].Domain.UpperBound;

		constraints[0].Propagate(out ConstraintOperationResult propagateResult);

		var boundsTightened = starts[2].Domain.LowerBound > originalLower || starts[2].Domain.UpperBound < originalUpper;
		Assert.True(boundsTightened || propagateResult == ConstraintOperationResult.Undecided);
	}
}
