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
	public class MetaExpressionInteger : ExpressionInteger, IMetaExpression<int>
	{
		private readonly IList<IVariable<int>> support;

		public IList<IVariable<int>> Support
		{
			get { return this.support; }
		}

		public MetaExpressionInteger(Expression<int> left, Expression<int> right, IEnumerable<IVariable<int>> support)
			: base(left, right)
		{
			this.support = support.ToList();
		}

		public MetaExpressionInteger(int integer, IEnumerable<IVariable<int>> support)
			: base(integer)
		{
			this.support = support.ToList();
		}

		internal MetaExpressionInteger(VariableInteger variable,
			Func<ExpressionInteger, ExpressionInteger, int> evaluate,
			Func<ExpressionInteger, ExpressionInteger, Bounds<int>> evaluateBounds,
			Func<ExpressionInteger, ExpressionInteger, Bounds<int>, ConstraintOperationResult> propagator,
			IEnumerable<IVariable<int>> support)
			: base(variable, evaluate, evaluateBounds, propagator)
		{
			this.support = support.ToList();
		}
	}
}
