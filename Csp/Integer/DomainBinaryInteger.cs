/*
  Copyright © Iain McDonald 2010-2019
  
  This file is part of Decider.
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Decider.Csp.BaseTypes;

namespace Decider.Csp.Integer
{
	public class DomainBinaryInteger : IDomain<int>
	{
		#region Underlying domain datatype
		
		//	IMPORTANT -	These lines must be updated together to reflect the correct size of bits per datatype
		private const int BitsPerDatatype = 8 * sizeof(uint);
		private uint[] domain;
		private const uint AllSet = 0xFFFFFFFF;
		
		#endregion

		private int lowerBound;
		private int upperBound;
		private int size;

		bool IDomain<int>.Contains(int index)
		{
			return IsInDomain(index);
		}

		private bool IsInDomain(int index)
		{
			return (this.domain[((index + 1) % BitsPerDatatype == 0) ?
				(index + 1) / BitsPerDatatype - 1 : (index + 1) / BitsPerDatatype] &
				(ulong) (0x1 << (index % BitsPerDatatype))) != 0;
		}

		private void RemoveFromDomain(int index)
		{
			this.domain[((index + 1) % BitsPerDatatype == 0) ? (index + 1) / BitsPerDatatype - 1 :
				(index + 1) / BitsPerDatatype] &= (uint) ~(0x1 << (index % BitsPerDatatype));
		}

		internal DomainBinaryInteger()
			: this(1)
		{
		}

		internal DomainBinaryInteger(int domainSize)
		{
			this.lowerBound = 0;
			this.upperBound = domainSize;
			this.size = upperBound - lowerBound + 1;
			this.domain = new uint[((domainSize + 1) % BitsPerDatatype == 0) ?
				(domainSize + 1) / BitsPerDatatype : (domainSize + 1) / BitsPerDatatype + 1];

			for (var i = 0; i < this.domain.Length - 1; ++i)
				this.domain[i] = AllSet;

			if ((domainSize + 1) % BitsPerDatatype == 0)
				this.domain[this.domain.Length - 1] = AllSet;
			else
				for (var i = 0; i < (domainSize + 1) % BitsPerDatatype; ++i)
					this.domain[this.domain.Length - 1] |= (uint) (0x1 << i);
		}

		internal DomainBinaryInteger(int lowerBound, int upperBound)
			: this(upperBound)
		{
			this.lowerBound = lowerBound;
			this.size = upperBound - lowerBound + 1;
			var count = 0;
			while (count < lowerBound)
				RemoveFromDomain(count++);
		}

		public static IDomain<int> CreateDomain(int lowerBound, int upperBound)
		{
			if (lowerBound > upperBound)
				throw new ArgumentException("Invalid Domain Bounds");

			var domainImpl = new DomainBinaryInteger(lowerBound, upperBound);
			return domainImpl;
		}

		public static IDomain<int> CreateDomain(IList<int> elements)
		{
			var lowerBound = elements.Min();
			var upperBound = elements.Max();

			var domainImpl = new DomainBinaryInteger(lowerBound, upperBound);

			for (var i = lowerBound; i <= upperBound; ++i)
				if (!elements.Contains(i))
					domainImpl.RemoveFromDomain(i);

			return domainImpl;
		}

		#region IDomain<int> Members

		int IDomain<int>.InstantiatedValue
		{
			get
			{
				if (!((IDomain<int>) this).Instantiated())
					throw new DeciderException("Trying to access InstantiatedValue of an uninstantiated domain.");

				return this.lowerBound;
			}
		}

		void IDomain<int>.Instantiate(out DomainOperationResult result)
		{
			((IDomain<int>) this).InstantiateLowest(out result);
		}

		void IDomain<int>.Instantiate(int value, out DomainOperationResult result)
		{
			if (!IsInDomain(value))
			{
				result = DomainOperationResult.ElementNotInDomain;
				return;
			}

			this.size = 1;
			this.lowerBound = this.upperBound = value;
			result = DomainOperationResult.InstantiateSuccessful;
		}

		void IDomain<int>.InstantiateLowest(out DomainOperationResult result)
		{
			if (!IsInDomain(this.lowerBound))
			{
				result = DomainOperationResult.ElementNotInDomain;
				return;
			}

			this.size = 1;
			this.upperBound = this.lowerBound;
			result = DomainOperationResult.InstantiateSuccessful;
		}

		void IDomain<int>.Remove(int element, out DomainOperationResult result)
		{
			result = DomainOperationResult.EmptyDomain;
			if (element < 0 || !IsInDomain(element))
			{
				result = DomainOperationResult.ElementNotInDomain;
				return;
			}

			RemoveFromDomain(element);

			if (this.size == 1)
			{
				this.size = 0;
				this.lowerBound = this.upperBound + 1;
				return;
			}

			if (element == this.lowerBound)
			{
				this.lowerBound = element;
				while (this.lowerBound <= this.upperBound && !IsInDomain(this.lowerBound))
				{
					++this.lowerBound;
					--this.size;
				}
			}
			else if (element == this.upperBound)
			{
				this.upperBound = element;
				while (this.upperBound >= this.lowerBound && !IsInDomain(this.upperBound))
				{
					--this.upperBound;
					--this.size;
				}
			}

			if (this.lowerBound > this.upperBound || this.size == 0)
				return;

			result = DomainOperationResult.RemoveSuccessful;
		}

		string IDomain<int>.ToString()
		{
			var domainRange = Enumerable.Range(lowerBound, upperBound - lowerBound + 1).Where(IsInDomain);

			return "[" + string.Join(", ", domainRange) + "]";
		}

		bool IDomain<int>.Instantiated()
		{
			return this.upperBound == this.lowerBound;
		}

		int IDomain<int>.Size()
		{
			return this.size;
		}

		int IDomain<int>.LowerBound
		{
			get { return this.lowerBound; }
		}

		int IDomain<int>.UpperBound
		{
			get { return this.upperBound; }
		}

		#endregion

		#region ICloneable Members

		public IDomain<int> Clone()
		{
			var clone = new DomainBinaryInteger { domain = new uint[this.domain.Length] };
			Array.Copy(this.domain, clone.domain, this.domain.Length);
			clone.lowerBound = this.lowerBound;
			clone.upperBound = this.upperBound;
			clone.size = this.size;

			return clone;
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			for (int i = this.lowerBound; i <= this.upperBound; ++i)
				if (IsInDomain(i))
					yield return i;
		}

		#endregion
	}
}
