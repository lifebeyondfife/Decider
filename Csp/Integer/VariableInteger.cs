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
		private readonly struct InstantiationEntry
		{
			internal readonly IDomain<int> Domain;
			internal readonly int Depth;
			internal readonly int OldGeneration;

			internal InstantiationEntry(IDomain<int> domain, int depth, int oldGeneration)
			{
				this.Domain = domain;
				this.Depth = depth;
				this.OldGeneration = oldGeneration;
			}
		}

		public IVariable<int> Clone()
		{
			var clonedStack = new Stack<InstantiationEntry>();
			foreach (var entry in this.instantiationStack.Reverse())
				clonedStack.Push(new InstantiationEntry(entry.Domain.Clone(), entry.Depth, entry.OldGeneration));

			return new VariableInteger
				{
					baseDomain = this.baseDomain.Clone(),
					instantiationStack = clonedStack,
					State = State,
					Name = Name,
					variableId = variableId
				};
		}

		private static int nextGeneration;

		private IDomain<int> baseDomain;
		private Stack<InstantiationEntry> instantiationStack;
		private int variableId;
		private int generation;

		public IState<int> State { get; set; }
		public string Name { get; private set; }
		internal int Generation { get { return this.generation; } }

		internal void RestoreGeneration(int oldGeneration)
		{
			this.generation = oldGeneration;
		}

		public IDomain<int> Domain
		{
			get
			{
				return this.instantiationStack.Count > 0
					? this.instantiationStack.Peek().Domain
					: this.baseDomain;
			}
		}

		internal IDomain<int> BaseDomain
		{
			get { return this.baseDomain; }
		}

		internal VariableInteger()
		{
			this.instantiationStack = new Stack<InstantiationEntry>();
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
			this.baseDomain = DomainBinaryInteger.CreateDomain(Int16.MinValue, Int16.MaxValue);
		}

		public VariableInteger(string name, IList<int> elements)
			: this()
		{
			this.Name = name;
			this.baseDomain = DomainBinaryInteger.CreateDomain(elements);
		}

		public VariableInteger(string name, int lowerBound, int upperBound)
			: this()
		{
			this.Name = name;
			this.baseDomain = DomainBinaryInteger.CreateDomain(lowerBound, upperBound);
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

			this.instantiationStack.Push(new InstantiationEntry(instantiatedDomain, depth, this.generation));
			this.generation = ++nextGeneration;
		}

		public void Instantiate(int value, int depth, out DomainOperationResult result)
		{
			var instantiatedDomain = this.Domain.Clone();
			instantiatedDomain.Instantiate(value, out result);
			if (result != DomainOperationResult.InstantiateSuccessful)
				return;

			this.instantiationStack.Push(new InstantiationEntry(instantiatedDomain, depth, this.generation));
			this.generation = ++nextGeneration;
		}

		public void Backtrack(int fromDepth)
		{
			while (this.instantiationStack.Count > 0 &&
			       this.instantiationStack.Peek().Depth >= fromDepth)
			{
				this.generation = this.instantiationStack.Pop().OldGeneration;
			}
		}

		private void RecordRemoval(int value, int depth)
		{
			if (this.instantiationStack.Count > 0)
				return;

			var domainBinary = (DomainBinaryInteger)this.baseDomain;
			var arrayIndex = domainBinary.GetArrayIndex(value);

			((StateInteger)this.State).Trail.RecordChange(
				this.variableId,
				arrayIndex,
				domainBinary.GetBits(arrayIndex),
				domainBinary.InternalLowerBound,
				domainBinary.InternalUpperBound,
				domainBinary.Size(),
				this.generation,
				depth
			);

			this.generation = ++nextGeneration;
		}

		public void Remove(int value, int depth, out DomainOperationResult result)
		{
			var domain = this.instantiationStack.Count == 0 ? this.baseDomain : this.instantiationStack.Peek().Domain;

			if (!domain.Contains(value))
			{
				result = DomainOperationResult.ElementNotInDomain;
				return;
			}

			RecordRemoval(value, depth);
			domain.Remove(value, out result);
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

		internal void SetVariableId(int id)
		{
			this.variableId = id;
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

			var domain = this.instantiationStack.Count == 0 ? this.baseDomain : this.instantiationStack.Peek().Domain;
			var depth = ((StateInteger)this.State).Depth;
			var domainResult = DomainOperationResult.RemoveSuccessful;

			while (enforceBounds.LowerBound > domain.LowerBound &&
				domainResult == DomainOperationResult.RemoveSuccessful)
			{
				var valueToRemove = domain.LowerBound;
				RecordRemoval(valueToRemove, depth);
				domain.Remove(valueToRemove, out domainResult);
				result = ConstraintOperationResult.Propagated;
			}

			while (enforceBounds.UpperBound < domain.UpperBound &&
				domainResult == DomainOperationResult.RemoveSuccessful)
			{
				var valueToRemove = domain.UpperBound;
				RecordRemoval(valueToRemove, depth);
				domain.Remove(valueToRemove, out domainResult);
				result = ConstraintOperationResult.Propagated;
			}
		}

		public override string ToString()
		{
			if (this.IsBound)
				return this.InstantiatedValue.ToString(CultureInfo.CurrentCulture);

			return this.Domain.ToString();
		}
	}
}
