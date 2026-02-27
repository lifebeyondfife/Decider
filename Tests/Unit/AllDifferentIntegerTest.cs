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

public class AllDifferentIntegerTest
{
	[Fact]
	public void TestImplementsIExplainableConstraint()
	{
		var x1 = new VariableInteger("x1", 1, 2);
		var constraint = new AllDifferentInteger([x1]);

		Assert.IsAssignableFrom<IExplainableConstraint>(constraint);
	}

	[Fact]
	public void TestExplainLowerBoundReturnsHallSetBounds()
	{
		var x1 = new VariableInteger("x1", 1, 2);
		var x2 = new VariableInteger("x2", 1, 2);
		var x3 = new VariableInteger("x3", 1, 3);
		var constraint = new AllDifferentInteger([x1, x2, x3]);
		_ = new StateInteger([x1, x2, x3], [constraint]);

		constraint.Propagate(out _);

		var reasons = new List<BoundReason>();
		((IExplainableConstraint) constraint).Explain(x3.VariableId, true, x3.Domain.LowerBound, reasons);

		Assert.Contains(reasons, r => r.VariableIndex == x1.VariableId && r.IsLowerBound);
		Assert.Contains(reasons, r => r.VariableIndex == x1.VariableId && !r.IsLowerBound);
		Assert.Contains(reasons, r => r.VariableIndex == x2.VariableId && r.IsLowerBound);
		Assert.Contains(reasons, r => r.VariableIndex == x2.VariableId && !r.IsLowerBound);
		Assert.DoesNotContain(reasons, r => r.VariableIndex == x3.VariableId && r.IsLowerBound);
	}

	[Fact]
	public void TestExplainUpperBoundReturnsHallSetBounds()
	{
		var x1 = new VariableInteger("x1", 2, 3);
		var x2 = new VariableInteger("x2", 2, 3);
		var x3 = new VariableInteger("x3", 1, 3);
		var constraint = new AllDifferentInteger([x1, x2, x3]);
		_ = new StateInteger([x1, x2, x3], [constraint]);

		constraint.Propagate(out _);

		var reasons = new List<BoundReason>();
		((IExplainableConstraint) constraint).Explain(x3.VariableId, false, x3.Domain.UpperBound, reasons);

		Assert.Contains(reasons, r => r.VariableIndex == x1.VariableId && r.IsLowerBound);
		Assert.Contains(reasons, r => r.VariableIndex == x1.VariableId && !r.IsLowerBound);
		Assert.Contains(reasons, r => r.VariableIndex == x2.VariableId && r.IsLowerBound);
		Assert.Contains(reasons, r => r.VariableIndex == x2.VariableId && !r.IsLowerBound);
		Assert.DoesNotContain(reasons, r => r.VariableIndex == x3.VariableId && !r.IsLowerBound);
	}

	[Fact]
	public void TestExplainExcludesNonHallSetVariables()
	{
		var x1 = new VariableInteger("x1", 1, 2);
		var x2 = new VariableInteger("x2", 1, 2);
		var x3 = new VariableInteger("x3", 1, 3);
		var x4 = new VariableInteger("x4", 4, 5);
		var constraint = new AllDifferentInteger([x1, x2, x3, x4]);
		_ = new StateInteger([x1, x2, x3, x4], [constraint]);

		constraint.Propagate(out _);

		var reasons = new List<BoundReason>();
		((IExplainableConstraint) constraint).Explain(x3.VariableId, true, x3.Domain.LowerBound, reasons);

		Assert.DoesNotContain(reasons, r => r.VariableIndex == x4.VariableId);
	}

	[Fact]
	public void TestPropagatesCorrectlyWithHallSet()
	{
		var x1 = new VariableInteger("x1", 1, 2);
		var x2 = new VariableInteger("x2", 1, 2);
		var x3 = new VariableInteger("x3", 1, 3);
		var constraint = new AllDifferentInteger([x1, x2, x3]);
		_ = new StateInteger([x1, x2, x3], [constraint]);

		constraint.Propagate(out var result);

		Assert.Equal(ConstraintOperationResult.Propagated, result);
		Assert.Equal(3, x3.Domain.LowerBound);
		Assert.Equal(3, x3.Domain.UpperBound);
	}

}
