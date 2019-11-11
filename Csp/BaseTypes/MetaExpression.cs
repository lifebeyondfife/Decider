/*
  Copyright © Iain McDonald 2010-2019
  
  This file is part of Decider.

  Unlike the Expression type which is wholly supported on its own, the MetaExpression relies
  on the values of other supporting variables. Thus, if those variables change, the bounds
  of the MetaExpression need to be re-evaluated.
*/
using System.Collections.Generic;

namespace Decider.Csp.BaseTypes
{
	public interface IMetaExpression<T>
	{
		IList<IVariable<T>> Support { get; }
	}
}
