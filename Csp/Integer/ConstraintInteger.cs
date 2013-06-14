/*
  Copyright © Iain McDonald 2010-2013
  
  This file is part of Decider.

	Decider is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	Decider is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with Decider.  If not, see <http://www.gnu.org/licenses/>.
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
				variableSet.Add((VariableInteger) expression.Left);
			else if (expression.Left is ExpressionInteger)
				ConstructVariableList((ExpressionInteger) expression.Left, variableSet);

			if (expression.Right is VariableInteger)
				variableSet.Add((VariableInteger) expression.Right);
			else if (expression.Right is ExpressionInteger)
				ConstructVariableList((ExpressionInteger) expression.Right, variableSet);
		}

		void IConstraint.Check(out ConstraintOperationResult result)
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

		void IConstraint.Propagate(out ConstraintOperationResult result)
		{
			var enforce = new Bounds<int>(1, 1);

			do
			{
				Propagate(enforce, out result);
			} while ((result &= ConstraintOperationResult.Propagated) == ConstraintOperationResult.Propagated);
		}

		bool IConstraint.StateChanged()
		{
			return this.variableArray.Where((variable, index) => ((VariableInteger) variable)
				.Domain != this.domainArray[index]).Any();
		}
	}
}

