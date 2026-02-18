/*
  Copyright © Iain McDonald 2010-2026

  This file is part of Decider.
*/
using Xunit;

using Decider.Csp.BaseTypes;
using Decider.Csp.Integer;

namespace Decider.Tests.Csp;

public class ExpressionIntegerTest
{
    private readonly Bounds<int> EnforceTrue = new Bounds<int>(1, 1);
    private readonly Bounds<int> EnforceFalse = new Bounds<int>(0, 0);

    [Fact]
    public void TestAddBoundsCorrect()
    {
        var variable1 = new VariableInteger("var1", 0, 5);
        var variable2 = new VariableInteger("var2", 0, 5);

        var expression = variable1 + variable2;
        var updatedBounds = expression.GetUpdatedBounds();

        Assert.Equal(0, updatedBounds.LowerBound);
        Assert.Equal(10, updatedBounds.UpperBound);
    }

    [Fact]
    public void TestSubtractBoundsCorrect()
    {
        var variable1 = new VariableInteger("var1", 0, 5);
        var variable2 = new VariableInteger("var2", 0, 5);

        var expression = variable1 - variable2;
        var updatedBounds = expression.GetUpdatedBounds();

        Assert.Equal(-5, updatedBounds.LowerBound);
        Assert.Equal(5, updatedBounds.UpperBound);
    }

    [Fact]
    public void TestMultiplyBoundsCorrect()
    {
        var variable1 = new VariableInteger("var1", 0, 5);
        var variable2 = new VariableInteger("var2", 0, 5);

        var expression = variable1 * variable2;
        var updatedBounds = expression.GetUpdatedBounds();

        Assert.Equal(0, updatedBounds.LowerBound);
        Assert.Equal(25, updatedBounds.UpperBound);
    }

    [Fact]
    public void TestDivideBoundsCorrect()
    {
        var divisor = new VariableInteger("divisor", 1, 5);

        var expression = 10 / divisor;
        var updatedBounds = expression.GetUpdatedBounds();

        Assert.Equal(2, updatedBounds.LowerBound);
        Assert.Equal(10, updatedBounds.UpperBound);
    }

    [Fact]
    public void TestXorBoundsCorrect()
    {
        var variable1 = new VariableInteger("var1", 0, 0);
        var variable2 = new VariableInteger("var2", 1, 1);

        var expression = variable1 ^ variable2;
        var updatedBounds = expression.GetUpdatedBounds();

        Assert.Equal(1, updatedBounds.LowerBound);
        Assert.Equal(1, updatedBounds.UpperBound);
    }

    [Fact]
    public void TestAndBoundsCorrectBothTrue()
    {
        var variable1 = new VariableInteger("var1", 1, 1);
        var variable2 = new VariableInteger("var2", 1, 1);

        var expression = variable1 & variable2;
        var updatedBounds = expression.GetUpdatedBounds();

        Assert.Equal(1, updatedBounds.LowerBound);
        Assert.Equal(1, updatedBounds.UpperBound);
    }

    [Fact]
    public void TestAndBoundsCorrectOneFalse()
    {
        var variable1 = new VariableInteger("var1", 1, 1);
        var variable2 = new VariableInteger("var2", 0, 0);

        var expression = variable1 & variable2;
        var updatedBounds = expression.GetUpdatedBounds();

        Assert.Equal(0, updatedBounds.LowerBound);
        Assert.Equal(0, updatedBounds.UpperBound);
    }

    [Fact]
    public void TestOrBoundsCorrectBothFalse()
    {
        var variable1 = new VariableInteger("var1", 0, 0);
        var variable2 = new VariableInteger("var2", 0, 0);

        var expression = variable1 | variable2;
        var updatedBounds = expression.GetUpdatedBounds();

        Assert.Equal(0, updatedBounds.LowerBound);
        Assert.Equal(0, updatedBounds.UpperBound);
    }

    [Fact]
    public void TestOrBoundsCorrectOneTrue()
    {
        var variable1 = new VariableInteger("var1", 0, 0);
        var variable2 = new VariableInteger("var2", 1, 1);

        var expression = variable1 | variable2;
        var updatedBounds = expression.GetUpdatedBounds();

        Assert.Equal(1, updatedBounds.LowerBound);
        Assert.Equal(1, updatedBounds.UpperBound);
    }

    [Fact]
    public void TestNotBoundsCorrectTrue()
    {
        var variable = new VariableInteger("var", 1, 1);

        var expression = !variable;
        var updatedBounds = expression.GetUpdatedBounds();

        Assert.Equal(0, updatedBounds.LowerBound);
        Assert.Equal(0, updatedBounds.UpperBound);
    }

    [Fact]
    public void TestNotBoundsCorrectFalse()
    {
        var variable = new VariableInteger("var", 0, 0);

        var expression = !variable;
        var updatedBounds = expression.GetUpdatedBounds();

        Assert.Equal(1, updatedBounds.LowerBound);
        Assert.Equal(1, updatedBounds.UpperBound);
    }

    [Fact]
    public void TestLessThanBoundsCorrectDefinitelyTrue()
    {
        var variable1 = new VariableInteger("var1", 0, 2);
        var variable2 = new VariableInteger("var2", 3, 5);

        var expression = variable1 < variable2;
        var updatedBounds = expression.GetUpdatedBounds();

        Assert.Equal(1, updatedBounds.LowerBound);
        Assert.Equal(1, updatedBounds.UpperBound);
    }

    [Fact]
    public void TestLessThanBoundsCorrectOverlapping()
    {
        var variable1 = new VariableInteger("var1", 0, 5);
        var variable2 = new VariableInteger("var2", 3, 8);

        var expression = variable1 < variable2;
        var updatedBounds = expression.GetUpdatedBounds();

        Assert.Equal(0, updatedBounds.LowerBound);
        Assert.Equal(1, updatedBounds.UpperBound);
    }

    [Fact]
    public void TestGreaterThanBoundsCorrectDefinitelyTrue()
    {
        var variable1 = new VariableInteger("var1", 6, 10);
        var variable2 = new VariableInteger("var2", 0, 5);

        var expression = variable1 > variable2;
        var updatedBounds = expression.GetUpdatedBounds();

        Assert.Equal(1, updatedBounds.LowerBound);
        Assert.Equal(1, updatedBounds.UpperBound);
    }

    [Fact]
    public void TestGreaterThanBoundsCorrectOverlapping()
    {
        var variable1 = new VariableInteger("var1", 0, 5);
        var variable2 = new VariableInteger("var2", 0, 5);

        var expression = variable1 > variable2;
        var updatedBounds = expression.GetUpdatedBounds();

        Assert.Equal(0, updatedBounds.LowerBound);
        Assert.Equal(1, updatedBounds.UpperBound);
    }

    [Fact]
    public void TestLessThanOrEqualBoundsCorrectDefinitelyTrue()
    {
        var variable1 = new VariableInteger("var1", 0, 3);
        var variable2 = new VariableInteger("var2", 3, 5);

        var expression = variable1 <= variable2;
        var updatedBounds = expression.GetUpdatedBounds();

        Assert.Equal(1, updatedBounds.LowerBound);
        Assert.Equal(1, updatedBounds.UpperBound);
    }

    [Fact]
    public void TestLessThanOrEqualBoundsCorrectOverlapping()
    {
        var variable1 = new VariableInteger("var1", 0, 5);
        var variable2 = new VariableInteger("var2", 3, 4);

        var expression = variable1 <= variable2;
        var updatedBounds = expression.GetUpdatedBounds();

        Assert.Equal(0, updatedBounds.LowerBound);
        Assert.Equal(1, updatedBounds.UpperBound);
    }

    [Fact]
    public void TestGreaterThanOrEqualBoundsCorrectDefinitelyTrue()
    {
        var variable1 = new VariableInteger("var1", 5, 10);
        var variable2 = new VariableInteger("var2", 0, 5);

        var expression = variable1 >= variable2;
        var updatedBounds = expression.GetUpdatedBounds();

        Assert.Equal(1, updatedBounds.LowerBound);
        Assert.Equal(1, updatedBounds.UpperBound);
    }

    [Fact]
    public void TestGreaterThanOrEqualBoundsCorrectOverlapping()
    {
        var variable1 = new VariableInteger("var1", 0, 5);
        var variable2 = new VariableInteger("var2", 3, 8);

        var expression = variable1 >= variable2;
        var updatedBounds = expression.GetUpdatedBounds();

        Assert.Equal(0, updatedBounds.LowerBound);
        Assert.Equal(1, updatedBounds.UpperBound);
    }

    [Fact]
    public void TestEqualBoundsCorrectDefinitelyEqual()
    {
        var variable1 = new VariableInteger("var1", 3, 3);
        var variable2 = new VariableInteger("var2", 3, 3);

        #pragma warning disable CS1718
        var expression = variable1 == variable2;
        #pragma warning restore CS1718
        var updatedBounds = expression.GetUpdatedBounds();

        Assert.Equal(1, updatedBounds.LowerBound);
        Assert.Equal(1, updatedBounds.UpperBound);
    }

    [Fact]
    public void TestEqualBoundsCorrectDefinitelyNotEqual()
    {
        var variable1 = new VariableInteger("var1", 0, 2);
        var variable2 = new VariableInteger("var2", 3, 5);

        #pragma warning disable CS1718
        var expression = variable1 == variable2;
        #pragma warning restore CS1718
        var updatedBounds = expression.GetUpdatedBounds();

        Assert.Equal(0, updatedBounds.LowerBound);
        Assert.Equal(0, updatedBounds.UpperBound);
    }

    [Fact]
    public void TestNotEqualBoundsCorrectDefinitelyNotEqual()
    {
        var variable1 = new VariableInteger("var1", 0, 2);
        var variable2 = new VariableInteger("var2", 3, 5);

        var expression = variable1 != variable2;
        var updatedBounds = expression.GetUpdatedBounds();

        Assert.Equal(1, updatedBounds.LowerBound);
        Assert.Equal(1, updatedBounds.UpperBound);
    }

    [Fact]
    public void TestNotEqualBoundsCorrectDefinitelyEqual()
    {
        var variable1 = new VariableInteger("var1", 3, 3);
        var variable2 = new VariableInteger("var2", 3, 3);

        var expression = variable1 != variable2;
        var updatedBounds = expression.GetUpdatedBounds();

        Assert.Equal(0, updatedBounds.LowerBound);
        Assert.Equal(0, updatedBounds.UpperBound);
    }

    [Fact]
    public void TestMultiplyBoundsCorrectNegativeRanges()
    {
        var variable1 = new VariableInteger("var1", -3, 2);
        var variable2 = new VariableInteger("var2", -4, 5);

        var expression = variable1 * variable2;
        var updatedBounds = expression.GetUpdatedBounds();

        Assert.Equal(-15, updatedBounds.LowerBound);
        Assert.Equal(12, updatedBounds.UpperBound);
    }

    /*
     * Not checking the `out var result` of expression.Propagte(), because Propagate only returns
     * Propagated when child expressions recursively propagate — which doesn't happen for bare 
     * VariableInteger without an attached State.
     */
    [Fact]
    public void TestAddPropagatorNarrowsBounds()
    {
        var variable1 = new VariableInteger("var1", 0, 3);
        var variable2 = new VariableInteger("var2", 0, 3);

        var expression = variable1 + variable2;
        expression.Propagate(new Bounds<int>(4, 6), out _);

        Assert.Equal(1, variable1.Bounds.LowerBound);
        Assert.Equal(1, variable2.Bounds.LowerBound);
    }

    [Fact]
    public void TestAddPropagatorDetectsViolation()
    {
        var variable1 = new VariableInteger("var1", 0, 2);
        var variable2 = new VariableInteger("var2", 0, 2);

        var expression = variable1 + variable2;
        expression.Propagate(new Bounds<int>(5, 5), out var result);

        Assert.Equal(ConstraintOperationResult.Violated, result);
    }

    [Fact]
    public void TestSubtractPropagatorNarrowsBounds()
    {
        var variable1 = new VariableInteger("var1", 0, 5);
        var variable2 = new VariableInteger("var2", 0, 5);

        var expression = variable1 - variable2;
        expression.Propagate(new Bounds<int>(2, 4), out _);

        Assert.Equal(2, variable1.Bounds.LowerBound);
        Assert.Equal(3, variable2.Bounds.UpperBound);
    }

    [Fact]
    public void TestLessThanPropagatorEnforceTrue()
    {
        var variable1 = new VariableInteger("var1", 0, 5);
        var variable2 = new VariableInteger("var2", 0, 5);

        var expression = variable1 < variable2;
        expression.Propagate(EnforceTrue, out _);

        Assert.Equal(4, variable1.Bounds.UpperBound);
        Assert.Equal(1, variable2.Bounds.LowerBound);
    }

    [Fact]
    public void TestGreaterThanPropagatorEnforceTrue()
    {
        var variable1 = new VariableInteger("var1", 0, 5);
        var variable2 = new VariableInteger("var2", 0, 5);

        var expression = variable1 > variable2;
        expression.Propagate(EnforceTrue, out _);

        Assert.Equal(1, variable1.Bounds.LowerBound);
        Assert.Equal(4, variable2.Bounds.UpperBound);
    }

    [Fact]
    public void TestEqualPropagatorEnforceTrueIntersectsBounds()
    {
        var variable1 = new VariableInteger("var1", 0, 3);
        var variable2 = new VariableInteger("var2", 2, 5);

        #pragma warning disable CS1718
        var expression = variable1 == variable2;
        #pragma warning restore CS1718
        expression.Propagate(EnforceTrue, out _);

        Assert.Equal(2, variable1.Bounds.LowerBound);
        Assert.Equal(3, variable1.Bounds.UpperBound);
        Assert.Equal(2, variable2.Bounds.LowerBound);
        Assert.Equal(3, variable2.Bounds.UpperBound);
    }

    [Fact]
    public void TestNotPropagatorEnforceTrueSetsUpperBoundZero()
    {
        var variable = new VariableInteger("var", 0, 1);

        var expression = !variable;
        expression.Propagate(EnforceTrue, out _);

        Assert.Equal(0, variable.Bounds.UpperBound);
    }

    [Fact]
    public void TestNotPropagatorEnforceFalseSetsLowerBoundOne()
    {
        var variable = new VariableInteger("var", 0, 1);

        var expression = !variable;
        expression.Propagate(EnforceFalse, out _);

        Assert.Equal(1, variable.Bounds.LowerBound);
    }
}
