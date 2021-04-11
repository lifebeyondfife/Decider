/*
  Copyright © Iain McDonald 2010-2021
  
  This file is part of Decider.
*/
using System;
using System.Collections.Generic;
using System.Linq;

using Decider.Csp.BaseTypes;

namespace Decider.Csp.Integer
{
	public class ConstraintInteger : ExpressionInteger, IConstraint
	{
		private readonly IVariable<int>[] variableArray;
		private readonly IDomain<int>[] domainArray;

		public ConstraintInteger(Expression<int> expression)
		{
			var expressionInt = (ExpressionInteger) expression;
			this.left = expressionInt.Left;
			this.right = expressionInt.Right;
			this.evaluate = expressionInt.Evaluate;
			this.evaluateBounds = expressionInt.EvaluateBounds;
			this.propagator = expressionInt.Propagator;
			this.integer = expressionInt.Integer;

			var variableSet = new HashSet<IVariable<int>>();
			ConstructVariableList((ExpressionInteger) expression, variableSet);
			this.variableArray = variableSet.ToArray();
			this.domainArray = new IDomain<int>[this.variableArray.Length];
		}

		private static void ConstructVariableList(ExpressionInteger expression, ISet<IVariable<int>> variableSet)
		{
			if (expression.Left is VariableInteger)
			{
				variableSet.Add((VariableInteger) expression.Left);
			}
			else if (expression.Left is MetaExpressionInteger)
			{
				ConstructVariableList((ExpressionInteger) expression.Left, variableSet);
				foreach (var variable in ((IMetaExpression<int>) expression.Left).Support)
				{
					variableSet.Add(variable);
				}
			}
			else if (expression.Left is ExpressionInteger)
			{
				ConstructVariableList((ExpressionInteger) expression.Left, variableSet);
			}


			if (expression.Right is VariableInteger)
			{
				variableSet.Add((VariableInteger) expression.Right);
			}
			else if (expression.Right is MetaExpressionInteger)
			{
				ConstructVariableList((ExpressionInteger) expression.Right, variableSet);
				foreach (var variable in ((IMetaExpression<int>) expression.Right).Support)
				{
					variableSet.Add(variable);
				}
			}
			else if (expression.Right is ExpressionInteger)
			{
				ConstructVariableList((ExpressionInteger) expression.Right, variableSet);
			}
		}

		public void Check(out ConstraintOperationResult result)
		{
			for (var i = 0; i < this.variableArray.Length; ++i)
				this.domainArray[i] = ((VariableInteger) variableArray[i]).Domain;

			if (this.variableArray.Any(variable => !variable.Instantiated()))
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
			var enforce = new Bounds<int>(1, 1);

			do
			{
				Propagate(enforce, out result);
			} while ((result &= ConstraintOperationResult.Propagated) == ConstraintOperationResult.Propagated);
		}

		public bool StateChanged()
		{
			return this.variableArray.Where((variable, index) => ((VariableInteger) variable)
				.Domain != this.domainArray[index]).Any();
		}
	}
}

