/*
  Copyright © Iain McDonald 2010-2026
  
  This file is part of Decider.
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Decider.Csp.BaseTypes;

namespace Decider.Csp.Integer;

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
	private int offset;

	public bool Contains(int index)
	{
		return IsInDomain(index);
	}

	private bool IsInDomain(int index)
	{
		index += offset;
		return (this.domain[((index + 1) % BitsPerDatatype == 0) ?
			(index + 1) / BitsPerDatatype - 1 : (index + 1) / BitsPerDatatype] &
			(ulong) (0x1 << (index % BitsPerDatatype))) != 0;
	}

	private void RemoveFromDomain(int index)
	{
		index += offset;
		this.domain[((index + 1) % BitsPerDatatype == 0) ? (index + 1) / BitsPerDatatype - 1 :
			(index + 1) / BitsPerDatatype] &= (uint) ~(0x1 << (index % BitsPerDatatype));
	}

	internal DomainBinaryInteger()
		: this(1)
	{
	}

	internal DomainBinaryInteger(int domainSize)
	{
		if (domainSize < 0)
			throw new ArgumentException("Invalid Domain Size");
		
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
		: this(upperBound + (lowerBound < 0 ? -lowerBound : 0))
	{
		if (lowerBound < 0)
			this.offset = -lowerBound;

		this.lowerBound = Math.Max(lowerBound, 0);
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

	public int InstantiatedValue
	{
		get
		{
			if (!Instantiated())
				throw new DeciderException("Trying to access InstantiatedValue of an uninstantiated domain.");

			return this.lowerBound - offset;
		}
	}

	public void Instantiate(out DomainOperationResult result)
	{
		InstantiateLowest(out result);
	}

	public void Instantiate(int value, out DomainOperationResult result)
	{
		if (!IsInDomain(value))
		{
			result = DomainOperationResult.ElementNotInDomain;
			return;
		}

		this.size = 1;
		this.lowerBound = this.upperBound = value + offset;
		result = DomainOperationResult.InstantiateSuccessful;
	}

	public void InstantiateLowest(out DomainOperationResult result)
	{
		if (!IsInDomain(this.lowerBound - offset))
		{
			result = DomainOperationResult.ElementNotInDomain;
			return;
		}

		this.size = 1;
		this.upperBound = this.lowerBound;
		result = DomainOperationResult.InstantiateSuccessful;
	}

	public void Remove(int element, out DomainOperationResult result)
	{
		result = DomainOperationResult.EmptyDomain;
		if (element < -offset || !IsInDomain(element))
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

		if (element + offset == this.lowerBound)
		{
			while (this.lowerBound <= this.upperBound && !IsInDomain(this.lowerBound - offset))
			{
				++this.lowerBound;
				--this.size;
			}
		}
		else if (element + offset == this.upperBound)
		{
			while (this.upperBound >= this.lowerBound && !IsInDomain(this.upperBound - offset))
			{
				--this.upperBound;
				--this.size;
			}
		}

		if (this.lowerBound > this.upperBound || this.size == 0)
			return;

		result = DomainOperationResult.RemoveSuccessful;
	}

	public override string ToString()
	{
		var domainRange = Enumerable.Range(lowerBound, upperBound - lowerBound + 1).Select(x => x - offset).Where(IsInDomain);

		return "[" + string.Join(", ", domainRange) + "]";
	}

	public bool Instantiated()
	{
		return this.upperBound == this.lowerBound;
	}

	public int Size()
	{
		return this.size;
	}

	public int LowerBound
	{
		get { return this.lowerBound - offset; }
	}

	public int UpperBound
	{
		get { return this.upperBound - offset; }
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
		clone.offset = this.offset;

		return clone;
	}

	#endregion

	#region IEnumerable Members

	public IEnumerator<int> GetEnumerator()
	{
		for (int i = this.lowerBound - offset; i <= this.upperBound - offset; ++i)
			if (IsInDomain(i))
				yield return i;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion

	#region Trail Support

	internal int GetArrayIndex(int value)
	{
		var index = value + offset;
		return ((index + 1) % BitsPerDatatype == 0) ?
			(index + 1) / BitsPerDatatype - 1 :
			(index + 1) / BitsPerDatatype;
	}

	internal uint GetBits(int arrayIndex)
	{
		return this.domain[arrayIndex];
	}

	internal int InternalLowerBound { get { return this.lowerBound; } }
	internal int InternalUpperBound { get { return this.upperBound; } }

	internal void RestoreBits(int arrayIndex, uint bits)
	{
		this.domain[arrayIndex] = bits;
	}

	internal void SetBounds(int lower, int upper, int size)
	{
		this.lowerBound = lower;
		this.upperBound = upper;
		this.size = size;
	}

	#endregion
}
