/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

using Decider.Csp.BaseTypes;
using Decider.Csp.Global;
using Decider.Csp.Integer;

namespace Decider.Tests.Csp;

public class ClauseLearningTest
{
	[Fact]
	public void TestPropagationTrailRecordAndRetrieve()
	{
		var trail = new PropagationTrail(10, 100);

		trail.RecordDecision(0, 3, 3, 0);

		Assert.Equal(2, trail.Count);

		ref var lb = ref trail.GetEntry(0);
		Assert.Equal(0, lb.VariableId);
		Assert.True(lb.IsLowerBound);
		Assert.Equal(3, lb.NewBound);
		Assert.Equal(0, lb.DecisionLevel);
		Assert.Equal(PropagationTrail.ReasonDecision, lb.ReasonKind);

		ref var ub = ref trail.GetEntry(1);
		Assert.Equal(0, ub.VariableId);
		Assert.False(ub.IsLowerBound);
		Assert.Equal(3, ub.NewBound);
	}

	[Fact]
	public void TestPropagationTrailBacktrack()
	{
		var trail = new PropagationTrail(10, 100);

		trail.RecordDecision(0, 1, 1, 0);
		trail.RecordPropagation(1, true, 2, 0, PropagationTrail.ReasonConstraint, 0);
		trail.RecordDecision(1, 3, 3, 1);
		trail.RecordPropagation(2, false, 4, 1, PropagationTrail.ReasonConstraint, 1);

		Assert.Equal(6, trail.Count);

		trail.Backtrack(0);

		Assert.Equal(3, trail.Count);
	}

	[Fact]
	public void TestPropagationTrailClear()
	{
		var trail = new PropagationTrail(10, 100);
		trail.RecordDecision(0, 1, 1, 0);
		trail.Clear();

		Assert.Equal(0, trail.Count);
	}

	[Fact]
	public void TestClauseLiteralFalsified()
	{
		var x = new VariableInteger("x", 1, 5);
		_ = new StateInteger([x], []);

		var literalGe3 = new BoundReason(0, true, 3);
		x.Remove(3, 0, out _);
		x.Remove(4, 0, out _);
		x.Remove(5, 0, out _);

		Assert.True(Clause.IsLiteralFalsified(literalGe3, x));
	}

	[Fact]
	public void TestClauseLiteralSatisfied()
	{
		var x = new VariableInteger("x", 3, 5);
		_ = new StateInteger([x], []);

		var literalGe3 = new BoundReason(0, true, 3);

		Assert.True(Clause.IsLiteralSatisfied(literalGe3, x));
	}

	[Fact]
	public void TestClauseStoreAddAndCount()
	{
		var x = new VariableInteger("x", 1, 10);
		var y = new VariableInteger("y", 1, 10);
		var variables = new List<IVariable<int>> { x, y };
		_ = new StateInteger(variables, []);

		var store = new ClauseStore();
		var literals = new[]
		{
			new BoundReason(0, true, 5),
			new BoundReason(1, false, 3)
		};

		store.AddClause(literals, variables);

		Assert.Equal(1, store.Count);
	}

	[Fact]
	public void TestClauseStoreUnitPropagation()
	{
		var x = new VariableInteger("x", 1, 10);
		var y = new VariableInteger("y", 1, 10);
		var variables = new List<IVariable<int>> { x, y };
		_ = new StateInteger(variables, []);

		var store = new ClauseStore();
		var literals = new[]
		{
			new BoundReason(0, true, 5),
			new BoundReason(1, false, 3)
		};

		store.AddClause(literals, variables);

		x.Remove(5, 0, out _);
		x.Remove(6, 0, out _);
		x.Remove(7, 0, out _);
		x.Remove(8, 0, out _);
		x.Remove(9, 0, out _);
		x.Remove(10, 0, out _);

		var conflict = store.NotifyBoundChange(0, true, variables,
			out var forcedLiteral, out var forcedClauseIndex);

		Assert.False(conflict);
		Assert.Equal(0, forcedClauseIndex);
		Assert.Equal(1, forcedLiteral.VariableIndex);
		Assert.False(forcedLiteral.IsLowerBound);
		Assert.Equal(3, forcedLiteral.BoundValue);
	}

	[Fact]
	public void TestClauseStoreClear()
	{
		var x = new VariableInteger("x", 1, 10);
		var variables = new List<IVariable<int>> { x };
		_ = new StateInteger(variables, []);

		var store = new ClauseStore();
		store.AddClause([new BoundReason(0, true, 5)], variables);
		store.Clear();

		Assert.Equal(0, store.Count);
	}

	[Fact]
	public void TestClauseStoreReduceDatabase()
	{
		var x = new VariableInteger("x", 1, 10);
		var y = new VariableInteger("y", 1, 10);
		var z = new VariableInteger("z", 1, 10);
		var variables = new List<IVariable<int>> { x, y, z };
		_ = new StateInteger(variables, []);

		var store = new ClauseStore();
		store.MaxClauses = 2;

		for (var i = 0; i < 5; ++i)
		{
			var literals = new[]
			{
				new BoundReason(0, true, i + 1),
				new BoundReason(1, false, i + 1),
				new BoundReason(2, true, i + 1)
			};

			var idx = store.AddClause(literals, variables);
			if (i < 2)
				store.BumpActivity(idx);
		}

		store.ReduceDatabase();

		Assert.True(store.Count < 5);
	}

	[Fact]
	public void TestSearchWithClauseLearningFindsSolution()
	{
		var x = new VariableInteger("x", 1, 4);
		var y = new VariableInteger("y", 1, 4);
		var z = new VariableInteger("z", 1, 4);

		var constraints = new List<IConstraint>
		{
			new ConstraintInteger(x != y),
			new ConstraintInteger(y != z),
			new ConstraintInteger(x != z)
		};

		var state = new StateInteger([x, y, z], constraints)
		{
			ClauseLearningEnabled = true
		};

		var result = state.Search();

		Assert.Equal(StateOperationResult.Solved, result);
	}

	[Fact]
	public void TestSearchWithClauseLearningDetectsUnsatisfiable()
	{
		var x = new VariableInteger("x", 1, 2);
		var y = new VariableInteger("y", 1, 2);
		var z = new VariableInteger("z", 1, 2);

		var constraints = new List<IConstraint>
		{
			new ConstraintInteger(x != y),
			new ConstraintInteger(y != z),
			new ConstraintInteger(x != z)
		};

		var state = new StateInteger([x, y, z], constraints)
		{
			ClauseLearningEnabled = true
		};

		var result = state.Search();

		Assert.Equal(StateOperationResult.Unsatisfiable, result);
	}

	[Fact]
	public void TestNQueens4WithClauseLearning()
	{
		const int n = 4;
		var queens = new VariableInteger[n];
		for (var i = 0; i < n; ++i)
			queens[i] = new VariableInteger($"q{i}", 0, n - 1);

		var constraints = new List<IConstraint>();
		for (var i = 0; i < n; ++i)
		{
			for (var j = i + 1; j < n; ++j)
			{
				constraints.Add(new ConstraintInteger(queens[i] != queens[j]));
				constraints.Add(new ConstraintInteger(queens[i] - queens[j] != j - i));
				constraints.Add(new ConstraintInteger(queens[j] - queens[i] != j - i));
			}
		}

		var state = new StateInteger(queens, constraints) { ClauseLearningEnabled = true };
		var result = state.Search();

		Assert.Equal(StateOperationResult.Solved, result);
	}

	[Fact]
	public void TestSearchAllSolutionsWithClauseLearning()
	{
		var xOff = new VariableInteger("x", 1, 3);
		var yOff = new VariableInteger("y", 1, 3);
		var constraintsOff = new List<IConstraint> { new ConstraintInteger(xOff != yOff) };
		var stateOff = new StateInteger([xOff, yOff], constraintsOff);
		stateOff.SearchAllSolutions();

		var xOn = new VariableInteger("x", 1, 3);
		var yOn = new VariableInteger("y", 1, 3);
		var constraintsOn = new List<IConstraint> { new ConstraintInteger(xOn != yOn) };
		var stateOn = new StateInteger([xOn, yOn], constraintsOn) { ClauseLearningEnabled = true };
		stateOn.SearchAllSolutions();

		Assert.Equal(stateOff.Solutions.Count, stateOn.Solutions.Count);
	}

	[Theory]
	[InlineData(3, 3)]
	[InlineData(4, 4)]
	[InlineData(5, 5)]
	[InlineData(4, 6)]
	public void TestAllDifferentSearchAllSolutionsWithClauseLearning(int variableCount, int domainMax)
	{
		var varsOff = Enumerable.Range(0, variableCount)
			.Select(i => new VariableInteger($"v{i}", 1, domainMax)).ToArray();
		var constraintsOff = new List<IConstraint>();
		for (var i = 0; i < variableCount; ++i)
			for (var j = i + 1; j < variableCount; ++j)
				constraintsOff.Add(new ConstraintInteger(varsOff[i] != varsOff[j]));
		var stateOff = new StateInteger(varsOff, constraintsOff);
		stateOff.SearchAllSolutions();

		var varsOn = Enumerable.Range(0, variableCount)
			.Select(i => new VariableInteger($"v{i}", 1, domainMax)).ToArray();
		var constraintsOn = new List<IConstraint>();
		for (var i = 0; i < variableCount; ++i)
			for (var j = i + 1; j < variableCount; ++j)
				constraintsOn.Add(new ConstraintInteger(varsOn[i] != varsOn[j]));
		var stateOn = new StateInteger(varsOn, constraintsOn) { ClauseLearningEnabled = true };
		stateOn.SearchAllSolutions();

		Assert.Equal(stateOff.Solutions.Count, stateOn.Solutions.Count);
	}

	[Fact]
	public void TestMinusSingleConstraintSearchAllSolutions()
	{
		var aOff = new VariableInteger("a", 0, 2);
		var bOff = new VariableInteger("b", 0, 2);
		var stateOff = new StateInteger([aOff, bOff],
			[new ConstraintInteger(aOff - bOff != 1)]);
		stateOff.SearchAllSolutions();

		var aOn = new VariableInteger("a", 0, 2);
		var bOn = new VariableInteger("b", 0, 2);
		var stateOn = new StateInteger([aOn, bOn],
			[new ConstraintInteger(aOn - bOn != 1)]) { ClauseLearningEnabled = true };
		stateOn.SearchAllSolutions();

		Assert.Equal(stateOff.Solutions.Count, stateOn.Solutions.Count);
	}

	[Theory]
	[InlineData(false, false, true)]
	[InlineData(false, true, false)]
	[InlineData(true, false, false)]
	[InlineData(true, true, false)]
	[InlineData(true, false, true)]
	[InlineData(false, true, true)]
	[InlineData(true, true, true)]
	public void TestThreeVarDiagonalSubsets(bool pair01, bool pair02, bool pair12)
	{
		IList<IConstraint> MakeConstraints(VariableInteger q0, VariableInteger q1, VariableInteger q2)
		{
			var c = new List<IConstraint>();
			if (pair01) { c.Add(new ConstraintInteger(q0 - q1 != 1)); c.Add(new ConstraintInteger(q1 - q0 != 1)); }
			if (pair02) { c.Add(new ConstraintInteger(q0 - q2 != 2)); c.Add(new ConstraintInteger(q2 - q0 != 2)); }
			if (pair12) { c.Add(new ConstraintInteger(q1 - q2 != 1)); c.Add(new ConstraintInteger(q2 - q1 != 1)); }
			return c;
		}

		var aOff = new VariableInteger("q0", 0, 2);
		var bOff = new VariableInteger("q1", 0, 2);
		var cOff = new VariableInteger("q2", 0, 2);
		var stateOff = new StateInteger([aOff, bOff, cOff], MakeConstraints(aOff, bOff, cOff));
		stateOff.SearchAllSolutions();

		var aOn = new VariableInteger("q0", 0, 2);
		var bOn = new VariableInteger("q1", 0, 2);
		var cOn = new VariableInteger("q2", 0, 2);
		var stateOn = new StateInteger([aOn, bOn, cOn], MakeConstraints(aOn, bOn, cOn))
			{ ClauseLearningEnabled = true };
		stateOn.SearchAllSolutions();

		Assert.Equal(stateOff.Solutions.Count, stateOn.Solutions.Count);
	}

	[Fact]
	public void TestMinusTwoConstraintsSearchAllSolutions()
	{
		var aOff = new VariableInteger("a", 0, 2);
		var bOff = new VariableInteger("b", 0, 2);
		var stateOff = new StateInteger([aOff, bOff],
			[new ConstraintInteger(aOff - bOff != 1),
			 new ConstraintInteger(bOff - aOff != 1)]);
		stateOff.SearchAllSolutions();

		var aOn = new VariableInteger("a", 0, 2);
		var bOn = new VariableInteger("b", 0, 2);
		var stateOn = new StateInteger([aOn, bOn],
			[new ConstraintInteger(aOn - bOn != 1),
			 new ConstraintInteger(bOn - aOn != 1)]) { ClauseLearningEnabled = true };
		stateOn.SearchAllSolutions();

		Assert.Equal(stateOff.Solutions.Count, stateOn.Solutions.Count);
	}

	[Theory]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(5)]
	public void TestDiagonalOnlySearchAllSolutionsWithClauseLearning(int n)
	{
		var varsOff = Enumerable.Range(0, n)
			.Select(i => new VariableInteger($"q{i}", 0, n - 1)).ToArray();
		var constraintsOff = new List<IConstraint>();
		for (var i = 0; i < n; ++i)
			for (var j = i + 1; j < n; ++j)
			{
				constraintsOff.Add(new ConstraintInteger(varsOff[i] - varsOff[j] != j - i));
				constraintsOff.Add(new ConstraintInteger(varsOff[j] - varsOff[i] != j - i));
			}
		var stateOff = new StateInteger(varsOff, constraintsOff);
		stateOff.SearchAllSolutions();

		var varsOn = Enumerable.Range(0, n)
			.Select(i => new VariableInteger($"q{i}", 0, n - 1)).ToArray();
		var constraintsOn = new List<IConstraint>();
		for (var i = 0; i < n; ++i)
			for (var j = i + 1; j < n; ++j)
			{
				constraintsOn.Add(new ConstraintInteger(varsOn[i] - varsOn[j] != j - i));
				constraintsOn.Add(new ConstraintInteger(varsOn[j] - varsOn[i] != j - i));
			}
		var stateOn = new StateInteger(varsOn, constraintsOn) { ClauseLearningEnabled = true };
		stateOn.SearchAllSolutions();

		Assert.Equal(stateOff.Solutions.Count, stateOn.Solutions.Count);
	}

	[Theory]
	[InlineData(4)]
	[InlineData(5)]
	[InlineData(6)]
	[InlineData(7)]
	public void TestNQueensSearchAllSolutionsWithClauseLearning(int n)
	{
		var queensOff = Enumerable.Range(0, n)
			.Select(i => new VariableInteger($"q{i}", 0, n - 1)).ToArray();
		var constraintsOff = new List<IConstraint>();
		for (var i = 0; i < n; ++i)
			for (var j = i + 1; j < n; ++j)
			{
				constraintsOff.Add(new ConstraintInteger(queensOff[i] != queensOff[j]));
				constraintsOff.Add(new ConstraintInteger(queensOff[i] - queensOff[j] != j - i));
				constraintsOff.Add(new ConstraintInteger(queensOff[j] - queensOff[i] != j - i));
			}
		var stateOff = new StateInteger(queensOff, constraintsOff);
		stateOff.SearchAllSolutions();

		var queensOn = Enumerable.Range(0, n)
			.Select(i => new VariableInteger($"q{i}", 0, n - 1)).ToArray();
		var constraintsOn = new List<IConstraint>();
		for (var i = 0; i < n; ++i)
			for (var j = i + 1; j < n; ++j)
			{
				constraintsOn.Add(new ConstraintInteger(queensOn[i] != queensOn[j]));
				constraintsOn.Add(new ConstraintInteger(queensOn[i] - queensOn[j] != j - i));
				constraintsOn.Add(new ConstraintInteger(queensOn[j] - queensOn[i] != j - i));
			}
		var stateOn = new StateInteger(queensOn, constraintsOn) { ClauseLearningEnabled = true };
		stateOn.SearchAllSolutions();

		Assert.Equal(stateOff.Solutions.Count, stateOn.Solutions.Count);
	}

	[Fact]
	public void TestClauseLearningOffByDefault()
	{
		var x = new VariableInteger("x", 1, 3);
		var state = new StateInteger([x], []);

		Assert.False(state.ClauseLearningEnabled);
	}

	[Fact]
	public void TestOptimisationWithClauseLearning()
	{
		var x = new VariableInteger("x", 1, 10);
		var y = new VariableInteger("y", 1, 10);

		var constraints = new List<IConstraint>
		{
			new ConstraintInteger(x + y > 5)
		};

		var state = new StateInteger([x, y], constraints) { ClauseLearningEnabled = true };
		var result = state.Search(x);

		Assert.Equal(StateOperationResult.Solved, result);
		Assert.NotNull(state.OptimalSolution);
	}

	[Fact]
	public void TestConstraintIntegerImplementsIExplainableConstraint()
	{
		var x = new VariableInteger("x", 1, 5);
		var y = new VariableInteger("y", 1, 5);
		var constraint = new ConstraintInteger(x + y == 10);

		Assert.IsAssignableFrom<IExplainableConstraint>(constraint);
	}

	[Fact]
	public void TestConstraintIntegerExplainPlus()
	{
		var a = new VariableInteger("a", 1, 5);
		var b = new VariableInteger("b", 1, 5);
		var constraint = new ConstraintInteger(a + b == 10);
		_ = new StateInteger([a, b], [constraint]);

		constraint.Propagate(out _);

		Assert.Equal(5, a.Domain.LowerBound);
		Assert.Equal(5, b.Domain.LowerBound);

		var explainable = (IExplainableConstraint) constraint;

		var aLbReasons = new List<BoundReason>();
		explainable.Explain(a.VariableId, true, 5, aLbReasons);
		Assert.NotEmpty(aLbReasons);
		Assert.Contains(aLbReasons, r => r.VariableIndex == b.VariableId && !r.IsLowerBound && r.BoundValue == 5);

		var bLbReasons = new List<BoundReason>();
		explainable.Explain(b.VariableId, true, 5, bLbReasons);
		Assert.NotEmpty(bLbReasons);
		Assert.Contains(bLbReasons, r => r.VariableIndex == a.VariableId && !r.IsLowerBound && r.BoundValue == 5);
	}

	[Fact]
	public void TestConstraintIntegerExplainMinus()
	{
		var a = new VariableInteger("a", 1, 10);
		var b = new VariableInteger("b", 1, 5);
		var constraint = new ConstraintInteger(a - b == 3);
		_ = new StateInteger([a, b], [constraint]);

		constraint.Propagate(out _);

		Assert.Equal(4, a.Domain.LowerBound);

		var explainable = (IExplainableConstraint) constraint;
		var reasons = new List<BoundReason>();
		explainable.Explain(a.VariableId, true, 4, reasons);

		Assert.NotEmpty(reasons);
		Assert.Contains(reasons, r => r.VariableIndex == b.VariableId);
	}

	[Fact]
	public void TestConstraintIntegerExplainNotEquals()
	{
		var a = new VariableInteger("a", 3, 3);
		var b = new VariableInteger("b", 1, 3);
		var constraint = new ConstraintInteger(a != b);
		_ = new StateInteger([a, b], [constraint]);

		constraint.Propagate(out _);

		Assert.Equal(2, b.Domain.UpperBound);

		var explainable = (IExplainableConstraint) constraint;
		var reasons = new List<BoundReason>();
		explainable.Explain(b.VariableId, false, 2, reasons);

		Assert.NotEmpty(reasons);
		Assert.Contains(reasons, r => r.VariableIndex == a.VariableId && r.IsLowerBound && r.BoundValue == 3);
		Assert.Contains(reasons, r => r.VariableIndex == a.VariableId && !r.IsLowerBound && r.BoundValue == 3);
	}

	[Fact]
	public void TestClauseLearningRejectsMiddleValueOrdering()
	{
		var a = new VariableInteger("a", 0, 10);
		var b = new VariableInteger("b", 0, 10);
		var state = new StateInteger([a, b], [new ConstraintInteger(a + b < 15)],
			valueOrdering: new MiddleValueOrdering()) { ClauseLearningEnabled = true };

		Assert.Throws<InvalidOperationException>(() => state.Search());
	}

	[Fact]
	public void TestClauseLearningRejectsMiddleValueOrderingSearchAllSolutions()
	{
		var a = new VariableInteger("a", 0, 10);
		var b = new VariableInteger("b", 0, 10);
		var state = new StateInteger([a, b], [new ConstraintInteger(a + b < 15)],
			valueOrdering: new MiddleValueOrdering()) { ClauseLearningEnabled = true };

		Assert.Throws<InvalidOperationException>(() => state.SearchAllSolutions());
	}

	[Theory]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(5)]
	[InlineData(6)]
	[InlineData(7)]
	public void TestAllDifferentIntegerOnlyWithClauseLearning(int n)
	{
		var varsOff = Enumerable.Range(0, n)
			.Select(i => new VariableInteger($"v{i}", 0, n - 1)).ToArray();
		var stateOff = new StateInteger(varsOff,
			[new AllDifferentInteger(varsOff)]);
		stateOff.SearchAllSolutions();

		var varsOn = Enumerable.Range(0, n)
			.Select(i => new VariableInteger($"v{i}", 0, n - 1)).ToArray();
		var stateOn = new StateInteger(varsOn,
			[new AllDifferentInteger(varsOn)]) { ClauseLearningEnabled = true };
		stateOn.SearchAllSolutions();

		Assert.Equal(stateOff.Solutions.Count, stateOn.Solutions.Count);
	}

	[Theory]
	[InlineData(4)]
	[InlineData(5)]
	[InlineData(6)]
	[InlineData(7)]
	public void TestNQueensWithAllDifferentAndClauseLearning(int n)
	{
		var queensOff = Enumerable.Range(0, n)
			.Select(i => new VariableInteger($"q{i}", 0, n - 1)).ToArray();
		var constraintsOff = new List<IConstraint> { new AllDifferentInteger(queensOff) };
		for (var i = 0; i < n; ++i)
			for (var j = i + 1; j < n; ++j)
			{
				constraintsOff.Add(new ConstraintInteger(queensOff[i] - queensOff[j] != j - i));
				constraintsOff.Add(new ConstraintInteger(queensOff[j] - queensOff[i] != j - i));
			}
		var stateOff = new StateInteger(queensOff, constraintsOff);
		stateOff.SearchAllSolutions();

		var queensOn = Enumerable.Range(0, n)
			.Select(i => new VariableInteger($"q{i}", 0, n - 1)).ToArray();
		var constraintsOn = new List<IConstraint> { new AllDifferentInteger(queensOn) };
		for (var i = 0; i < n; ++i)
			for (var j = i + 1; j < n; ++j)
			{
				constraintsOn.Add(new ConstraintInteger(queensOn[i] - queensOn[j] != j - i));
				constraintsOn.Add(new ConstraintInteger(queensOn[j] - queensOn[i] != j - i));
			}
		var stateOn = new StateInteger(queensOn, constraintsOn) { ClauseLearningEnabled = true };
		stateOn.SearchAllSolutions();

		Assert.Equal(stateOff.Solutions.Count, stateOn.Solutions.Count);
	}

}
