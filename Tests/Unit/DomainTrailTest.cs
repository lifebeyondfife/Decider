/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System.Collections.Generic;
using System.Linq;
using Xunit;

using Decider.Csp.BaseTypes;
using Decider.Csp.Integer;

namespace Decider.Tests.Csp;

public class DomainTrailTest
{
    [Fact]
    public void TestSearchFindsSolutionWithTrail()
    {
        var x = new VariableInteger("x", 1, 3);
        var y = new VariableInteger("y", 1, 3);

        var constraints = new List<IConstraint>
        {
            new ConstraintInteger(x != y)
        };

        var state = new StateInteger(new IVariable<int>[] { x, y }, constraints);
        var result = state.Search();

        Assert.Equal(StateOperationResult.Solved, result);
    }

    [Fact]
    public void TestSearchAllSolutionsMatchesExpected()
    {
        var x = new VariableInteger("x", 1, 3);
        var y = new VariableInteger("y", 1, 3);

        var constraints = new List<IConstraint>
        {
            new ConstraintInteger(x != y)
        };

        var state = new StateInteger(new IVariable<int>[] { x, y }, constraints);
        var result = state.SearchAllSolutions();

        Assert.Equal(StateOperationResult.Solved, result);
        Assert.Equal(6, state.Solutions.Count);
    }

    [Fact]
    public void TestBacktrackingProducesCorrectResults()
    {
        var a = new VariableInteger("a", 1, 2);
        var b = new VariableInteger("b", 1, 2);
        var c = new VariableInteger("c", 1, 2);

        var constraints = new List<IConstraint>
        {
            new ConstraintInteger(a != b),
            new ConstraintInteger(b != c),
            new ConstraintInteger(a != c)
        };

        var state = new StateInteger(new IVariable<int>[] { a, b, c }, constraints);
        var result = state.SearchAllSolutions();

        Assert.Equal(StateOperationResult.Unsatisfiable, result);
        Assert.Empty(state.Solutions);
    }

    [Fact]
    public void TestAllDifferentWithTrail()
    {
        var variables = Enumerable.Range(0, 4)
            .Select(i => new VariableInteger($"v{i}", 1, 4))
            .ToArray();

        var constraints = new List<IConstraint>
        {
            new global::Decider.Csp.Global.AllDifferentInteger(variables)
        };

        var state = new StateInteger(
            variables.Cast<IVariable<int>>().ToArray(), constraints);
        var result = state.SearchAllSolutions();

        Assert.Equal(StateOperationResult.Solved, result);
        Assert.Equal(24, state.Solutions.Count);
    }

    [Fact]
    public void TestNegativeDomainWithTrail()
    {
        var x = new VariableInteger("x", -3, 3);
        var y = new VariableInteger("y", -3, 3);

        var constraints = new List<IConstraint>
        {
            new ConstraintInteger(x + y == 0),
            new ConstraintInteger(x != y)
        };

        var state = new StateInteger(new IVariable<int>[] { x, y }, constraints);
        var result = state.SearchAllSolutions();

        Assert.Equal(StateOperationResult.Solved, result);
        foreach (var solution in state.Solutions)
        {
            var xVal = solution["x"].InstantiatedValue;
            var yVal = solution["y"].InstantiatedValue;
            Assert.Equal(0, xVal + yVal);
            Assert.NotEqual(xVal, yVal);
        }
    }

    [Fact]
    public void TestDeepSearchWithBacktracking()
    {
        var variables = Enumerable.Range(0, 6)
            .Select(i => new VariableInteger($"v{i}", 1, 6))
            .ToArray();

        var constraints = new List<IConstraint>
        {
            new global::Decider.Csp.Global.AllDifferentInteger(variables)
        };

        var state = new StateInteger(
            variables.Cast<IVariable<int>>().ToArray(), constraints);
        var result = state.SearchAllSolutions();

        Assert.Equal(StateOperationResult.Solved, result);
        Assert.Equal(720, state.Solutions.Count);
    }

    [Fact]
    public void TestSingleElementDomain()
    {
        var x = new VariableInteger("x", new List<int> { 42 });
        var y = new VariableInteger("y", 1, 3);

        var constraints = new List<IConstraint>
        {
            new ConstraintInteger(x + y > 43)
        };

        var state = new StateInteger(new IVariable<int>[] { x, y }, constraints);
        var result = state.SearchAllSolutions();

        Assert.Equal(StateOperationResult.Solved, result);
        foreach (var solution in state.Solutions)
        {
            Assert.Equal(42, solution["x"].InstantiatedValue);
            Assert.True(solution["y"].InstantiatedValue >= 2);
        }
    }

    [Fact]
    public void TestSparseElementDomain()
    {
        var x = new VariableInteger("x", new List<int> { 1, 3, 5, 7, 9 });
        var y = new VariableInteger("y", new List<int> { 2, 4, 6, 8, 10 });

        var constraints = new List<IConstraint>
        {
            new ConstraintInteger(x + y == 11)
        };

        var state = new StateInteger(new IVariable<int>[] { x, y }, constraints);
        var result = state.SearchAllSolutions();

        Assert.Equal(StateOperationResult.Solved, result);
        foreach (var solution in state.Solutions)
        {
            Assert.Equal(11, solution["x"].InstantiatedValue + solution["y"].InstantiatedValue);
        }
    }
}
