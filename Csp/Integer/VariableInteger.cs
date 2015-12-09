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
using System.Globalization;
using System.Linq;
using System.Collections.Generic;

using Decider.Csp.BaseTypes;

namespace Decider.Csp.Integer
{
	public sealed class VariableInteger : ExpressionInteger, IVariable<int>
	{
		private struct DomInt : ICloneable
		{
			internal readonly IDomain<int> Domain;
			internal readonly int Depth;

			internal DomInt(IDomain<int> domain, int depth)
			{
				this.Domain = domain;
				this.Depth = depth;
			}

			public object Clone()
			{
				return new DomInt((IDomain<int>) this.Domain.Clone(), this.Depth);
			}
		}

		public object Clone()
		{
			return new VariableInteger
				{
					domainStack = new Stack<DomInt>(domainStack.Select(d => d.Clone()).Reverse().Cast<DomInt>()),
					State = State,
					Name = Name
				};
		}

		private Stack<DomInt> domainStack;
		public IState<int> State { get; set; }
		public string Name { get; private set; }
		public IDomain<int> Domain { get { return this.domainStack.Peek().Domain; } }

		internal VariableInteger()
		{
			this.remove = prune =>
			{
				DomainOperationResult result;
				((IVariable<int>) this).Remove(prune, out result);
				return result;
			};
		}

		public VariableInteger(string name, IList<int> elements)
			: this()
		{
			this.Name = name;
			this.domainStack = new Stack<DomInt>();
			this.domainStack.Push(new DomInt(DomainBinaryInteger.CreateDomain(elements), -1));
		}

		public VariableInteger(string name, int lowerBound, int upperBound)
			: this()
		{
			this.Name = name;
			this.domainStack = new Stack<DomInt>();
			this.domainStack.Push(new DomInt(DomainBinaryInteger.CreateDomain(lowerBound, upperBound), -1));
		}

		int IVariable<int>.InstantiatedValue
		{
			get
			{
				return this.Domain.InstantiatedValue;
			}
		}

		void IVariable<int>.Instantiate(int depth, out DomainOperationResult result)
		{
			var instantiatedDomain = (IDomain<int>) this.Domain.Clone();
			instantiatedDomain.Instantiate(out result);
			if (result != DomainOperationResult.InstantiateSuccessful)
				throw new ApplicationException("Failed to instantiate Variable.");

			this.domainStack.Push(new DomInt(instantiatedDomain, depth));
		}

		void IVariable<int>.Instantiate(int value, int depth, out DomainOperationResult result)
		{
			var instantiatedDomain = (IDomain<int>) this.Domain.Clone();
			instantiatedDomain.Instantiate(value, out result);
			if (result != DomainOperationResult.InstantiateSuccessful)
				throw new ApplicationException("Failed to instantiate Variable.");

			this.domainStack.Push(new DomInt(instantiatedDomain, depth));
		}

		void IVariable<int>.Backtrack(int fromDepth)
		{
			while (this.domainStack.Peek().Depth >= fromDepth)
				this.domainStack.Pop();
		}

		void IVariable<int>.Remove(int value, int depth, out DomainOperationResult result)
		{
			if (this.domainStack.Peek().Depth != depth)
			{
				this.domainStack.Push(new DomInt((IDomain<int>) this.Domain.Clone(), depth));

				this.Domain.Remove(value, out result);

				if (result == DomainOperationResult.ElementNotInDomain)
					this.domainStack.Pop();
			}
			else
				this.Domain.Remove(value, out result);
		}

		void IVariable<int>.Remove(int value, out DomainOperationResult result)
		{
			if (((IVariable<int>) this).Instantiated() || value > this.Domain.UpperBound || value < this.Domain.LowerBound)
			{
				result = DomainOperationResult.ElementNotInDomain;
				return;
			}

			((IVariable<int>) this).Remove(value, this.State.Depth, out result);
		}

		string IVariable<int>.ToString()
		{
			return this.Domain.ToString();
		}

		bool IVariable<int>.Instantiated()
		{
			return this.Domain.Instantiated();
		}

		int IVariable<int>.Size()
		{
			return this.Domain.Size();
		}

		void IVariable<int>.SetState(IState<int> state)
		{
			this.State = state;
		}

		int IComparable<IVariable<int>>.CompareTo(IVariable<int> otherVariable)
		{
			return ((IVariable<int>) this).Size() - otherVariable.Size();
		}

		public override int Value
		{
			get { return ((IVariable<int>) this).InstantiatedValue; }
		}

		public override bool IsBound
		{
			get { return ((IVariable<int>) this).Instantiated(); }
		}

		public override Bounds<int> GetUpdatedBounds()
		{
			this.Bounds = new Bounds<int>(this.Domain.LowerBound, this.Domain.UpperBound);
			return this.Bounds;
		}

		public override void Propagate(Bounds<int> enforceBounds, out ConstraintOperationResult result)
		{
			result = ConstraintOperationResult.Undecided;

			if (this.State == null)
				return;

			var domainIntStack = this.domainStack.Peek();
			var isDomainNew = false;
			IDomain<int> propagatedDomain;

			if (domainIntStack.Depth == this.State.Depth)
			{
				propagatedDomain = domainIntStack.Domain;
			}
			else
			{
				isDomainNew = true;
				propagatedDomain = (IDomain<int>) domainIntStack.Domain.Clone();
				this.domainStack.Push(new DomInt(propagatedDomain, this.State.Depth));
			}

			var domainResult = DomainOperationResult.RemoveSuccessful;

			while (enforceBounds.LowerBound > propagatedDomain.LowerBound &&
				domainResult == DomainOperationResult.RemoveSuccessful)
			{
				propagatedDomain.Remove(propagatedDomain.LowerBound, out domainResult);
				result = ConstraintOperationResult.Propagated;
			}

			while (enforceBounds.UpperBound < propagatedDomain.UpperBound &&
				domainResult == DomainOperationResult.RemoveSuccessful)
			{
				propagatedDomain.Remove(propagatedDomain.UpperBound, out domainResult);
				result = ConstraintOperationResult.Propagated;
			}

			if (isDomainNew && result != ConstraintOperationResult.Propagated)
				this.domainStack.Pop();
		}


		public override string ToString()
		{
			//string value = this.IsBound ? ((IVariable<int>) this).InstantiatedValue.ToString() : this.Domain.ToString();
			//return this.name + ": " + value;

			if (this.IsBound)
				return ((IVariable<int>) this).InstantiatedValue.ToString(CultureInfo.CurrentCulture);

			return this.Domain.ToString();
		}
	}
}
