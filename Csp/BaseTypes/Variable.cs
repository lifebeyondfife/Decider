/*
  Copyright © Iain McDonald 2010-2022

  This file is part of Decider.
*/
using System;

namespace Decider.Csp.BaseTypes;

public interface IVariable<T> : IComparable<IVariable<T>>
{
	void Instantiate(int depth, out DomainOperationResult result);
	void Instantiate(T value, int depth, out DomainOperationResult result);

	void Backtrack(int fromDepth);
	void Remove(T value, int depth, out DomainOperationResult result);
	void Remove(T value, out DomainOperationResult result);

	void SetState(IState<T> state);

	string ToString();
	string Name { get; }
	T InstantiatedValue { get; }
	bool Instantiated();
	int Size();
    IVariable<T> Clone();
}
