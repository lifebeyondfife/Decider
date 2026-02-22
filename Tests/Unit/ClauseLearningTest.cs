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
}
