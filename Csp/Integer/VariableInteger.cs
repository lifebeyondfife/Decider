/*
  Copyright © Iain McDonald 2010-2022
  
  This file is part of Decider.
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
		private struct DomInt
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
				return new DomInt(this.Domain.Clone(), this.Depth);
			}
		}

		public IVariable<int> Clone()
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
				Remove(prune, out result);
				return result;
			};
		}

		public VariableInteger(string name)
			: this()
		{
			this.Name = name;
			this.domainStack = new Stack<DomInt>();
			this.domainStack.Push(new DomInt(DomainBinaryInteger.CreateDomain(Int16.MinValue, Int16.MaxValue), -1));
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

		public int InstantiatedValue
		{
			get
			{
				return this.Domain.InstantiatedValue;
			}
		}

		public void Instantiate(int depth, out DomainOperationResult result)
		{
			var instantiatedDomain = this.Domain.Clone();
			instantiatedDomain.Instantiate(out result);
			if (result != DomainOperationResult.InstantiateSuccessful)
				return;

			this.domainStack.Push(new DomInt(instantiatedDomain, depth));
		}

		public void Instantiate(int value, int depth, out DomainOperationResult result)
		{
			var instantiatedDomain = this.Domain.Clone();
			instantiatedDomain.Instantiate(value, out result);
			if (result != DomainOperationResult.InstantiateSuccessful)
				return;

			this.domainStack.Push(new DomInt(instantiatedDomain, depth));
		}

		public void Backtrack(int fromDepth)
		{
			while (this.domainStack.Peek().Depth >= fromDepth)
				this.domainStack.Pop();
		}

		public void Remove(int value, int depth, out DomainOperationResult result)
		{
			if (this.domainStack.Peek().Depth != depth)
			{
				this.domainStack.Push(new DomInt(this.Domain.Clone(), depth));

				this.Domain.Remove(value, out result);

				if (result == DomainOperationResult.ElementNotInDomain)
					this.domainStack.Pop();
			}
			else
				this.Domain.Remove(value, out result);
		}

		public void Remove(int value, out DomainOperationResult result)
		{
			if (Instantiated() || value > this.Domain.UpperBound || value < this.Domain.LowerBound)
			{
				result = DomainOperationResult.ElementNotInDomain;
				return;
			}

			Remove(value, this.State.Depth, out result);
		}

		public bool Instantiated()
		{
			return this.Domain.Instantiated();
		}

		public int Size()
		{
			return this.Domain.Size();
		}

		public void SetState(IState<int> state)
		{
			this.State = state;
		}

		public int CompareTo(IVariable<int> otherVariable)
		{
			return Size() - otherVariable.Size();
		}

		public override int Value
		{
			get { return this.InstantiatedValue; }
		}

		public override bool IsBound
		{
			get { return Instantiated(); }
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
				propagatedDomain = domainIntStack.Domain.Clone();
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
			if (this.IsBound)
				return this.InstantiatedValue.ToString(CultureInfo.CurrentCulture);

			return this.Domain.ToString();
		}
	}
}
