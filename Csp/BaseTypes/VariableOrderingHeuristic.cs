/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System.Collections.Generic;

namespace Decider.Csp.BaseTypes;

public interface IVariableOrderingHeuristic<T>
{
	int SelectVariableIndex(IList<IVariable<T>> variables);
}
