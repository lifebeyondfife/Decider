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
using System.Collections;
using System.Runtime.Serialization;

namespace Decider.Csp.BaseTypes
{
	public enum DomainOperationResult
	{
		EmptyDomain,
		ElementNotInDomain,
		RemoveSuccessful,
		InstantiateSuccessful
	}

	public interface IDomain<T> : ICloneable, ISerializable, IEnumerable
	{
		T InstantiatedValue { get; }

		void Instantiate(out DomainOperationResult result);
		void Instantiate(T value, out DomainOperationResult result);
		void InstantiateLowest(out DomainOperationResult result);

		void Remove(T element, out DomainOperationResult result);
		bool Contains(T element);

		string ToString();
		bool Instantiated();
		T Size();
		T LowerBound { get; }
		T UpperBound { get; }
	}
}
