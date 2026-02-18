/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
namespace Decider.Csp.BaseTypes;

public interface IValueOrderingHeuristic<T>
{
	T SelectValue(IVariable<T> variable);
}
