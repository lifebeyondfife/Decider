/*
  Copyright © Iain McDonald 2010-2026

  This file is part of Decider.
*/
using System;
using System.Collections.Generic;
using System.Linq;

using Decider.Csp.BaseTypes;

namespace Decider.Csp.Integer;

public class ConstraintInteger : ExpressionInteger, IConstraint<int>, IExplainableConstraint
{
	private VariableInteger[] VariableList { get; set; } = Array.Empty<VariableInteger>();
	private IList<int> GenerationList { get; set; }
	private int[] SnapshotLB { get; set; } = Array.Empty<int>();
	private int[] SnapshotUB { get; set; } = Array.Empty<int>();

	public IReadOnlyList<IVariable<int>> Variables => this.VariableList;
	public int FailureWeight { get; set; }

	public ConstraintInteger(Expression<int> expression)
	{
		var expressionInt = (ExpressionInteger) expression;
		this.left = expressionInt.Left;
		this.right = expressionInt.Right;
		this.evaluate = expressionInt.Evaluate;
		this.evaluateBounds = expressionInt.EvaluateBounds;
		this.propagator = expressionInt.Propagator;
		this.integer = expressionInt.Integer;

		var variableSet = new HashSet<VariableInteger>();
		ConstructVariableList((ExpressionInteger) expression, variableSet);
		this.VariableList = variableSet.ToArray();
		this.GenerationList = new int[this.VariableList.Length];
		this.SnapshotLB = new int[this.VariableList.Length];
		this.SnapshotUB = new int[this.VariableList.Length];
	}

	private static void ConstructVariableList(ExpressionInteger expression, ISet<VariableInteger> variableSet)
	{
		if (expression.Left is VariableInteger leftVar)
		{
			variableSet.Add(leftVar);
		}
		else if (expression.Left is MetaExpressionInteger)
		{
			ConstructVariableList((ExpressionInteger) expression.Left, variableSet);
			foreach (var variable in ((IMetaExpression<int>) expression.Left).Support)
				variableSet.Add((VariableInteger) variable);
		}
		else if (expression.Left is ExpressionInteger)
		{
			ConstructVariableList((ExpressionInteger) expression.Left, variableSet);
		}

		if (expression.Right is VariableInteger rightVar)
		{
			variableSet.Add(rightVar);
		}
		else if (expression.Right is MetaExpressionInteger)
		{
			ConstructVariableList((ExpressionInteger) expression.Right, variableSet);
			foreach (var variable in ((IMetaExpression<int>) expression.Right).Support)
				variableSet.Add((VariableInteger) variable);
		}
		else if (expression.Right is ExpressionInteger)
		{
			ConstructVariableList((ExpressionInteger) expression.Right, variableSet);
		}
	}

	public void Check(out ConstraintOperationResult result)
	{
		for (var i = 0; i < this.VariableList.Length; ++i)
			this.GenerationList[i] = this.VariableList[i].Generation;

		if (this.VariableList.Any(variable => !variable.Instantiated()))
		{
			result = ConstraintOperationResult.Undecided;
			return;
		}

		try
		{
			result = this.Value != 0 ? ConstraintOperationResult.Satisfied : ConstraintOperationResult.Violated;
		}
		catch (DivideByZeroException)
		{
			result = ConstraintOperationResult.Violated;
		}
	}

	public void Propagate(out ConstraintOperationResult result)
	{
		for (var i = 0; i < this.VariableList.Length; ++i)
		{
			this.SnapshotLB[i] = this.VariableList[i].Domain.LowerBound;
			this.SnapshotUB[i] = this.VariableList[i].Domain.UpperBound;
		}

		var enforce = new Bounds<int>(1, 1);

		do
		{
			Propagate(enforce, out result);
		} while ((result & ConstraintOperationResult.Propagated) == ConstraintOperationResult.Propagated);
	}

	public void Explain(int variableId, bool isLowerBound, int boundValue, IList<BoundReason> result)
	{
		for (var i = 0; i < this.VariableList.Length; ++i)
		{
			var v = this.VariableList[i];
			if (v.VariableId == variableId)
			{
				result.Add(isLowerBound
					? new BoundReason(v.VariableId, true, this.SnapshotLB[i])
					: new BoundReason(v.VariableId, false, this.SnapshotUB[i]));
			}
			else
			{
				result.Add(new BoundReason(v.VariableId, true, this.SnapshotLB[i]));
				result.Add(new BoundReason(v.VariableId, false, this.SnapshotUB[i]));
			}
		}
	}

	public bool StateChanged()
	{
		return this.VariableList.Where((variable, index) =>
			variable.Generation != this.GenerationList[index]).Any();
	}
}
