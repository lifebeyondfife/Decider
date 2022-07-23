/*
  Copyright © Iain McDonald 2010-2022
  
  This file is part of Decider.
*/
using System.Collections;

namespace Decider.Csp.BaseTypes
{
	public enum DomainOperationResult
	{
		EmptyDomain,
		ElementNotInDomain,
		RemoveSuccessful,
		InstantiateSuccessful
	}

	public interface IDomain<T> : IEnumerable
	{
		T InstantiatedValue { get; }

		void Instantiate(out DomainOperationResult result);
		void Instantiate(T value, out DomainOperationResult result);
		void InstantiateLowest(out DomainOperationResult result);

		void Remove(T element, out DomainOperationResult result);
		bool Contains(T element);

		string ToString();
		bool Instantiated();
		int Size();
		T LowerBound { get; }
		T UpperBound { get; }
        IDomain<T> Clone();
	}
}
