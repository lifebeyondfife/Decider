/*
  Copyright © Iain McDonald 2010-2022
  
  This file is part of Decider.
*/
using Decider.Csp.Integer;
using System.Collections.Generic;
using System.Linq;

namespace Decider.Csp.BaseTypes
{
	public class ConstrainedArray : List<int>
	{
		private VariableInteger Index { get; set; }

		public MetaExpressionInteger this[VariableInteger index]
		{
			get
			{
				Index = index;

				return new MetaExpressionInteger(GetVariableInteger(), this.Evaluate, this.EvaluateBounds, this.Propagator, new[] { Index });
			}
		}

		public ConstrainedArray(IEnumerable<int> elements)
		{
			this.AddRange(elements);
		}

		private VariableInteger GetVariableInteger()
		{
			return new VariableInteger(Index.Name + this.ToString(), Elements());
		}

		private List<int> Elements()
		{
			return Enumerable.Range(Index.Domain.LowerBound, Index.Domain.UpperBound - Index.Domain.LowerBound + 1).
				Where(i => Index.Domain.Contains(i)).
				Select(i => this[i]).
				ToList();
		}

		private SortedList<int, IList<int>> SortedElements()
		{
			var kvps = Enumerable.Range(Index.Domain.LowerBound, Index.Domain.UpperBound - Index.Domain.LowerBound + 1).
				Where(i => Index.Domain.Contains(i)).
				Select(i => new { Index = this[i], Value = i });

			var sortedList = new SortedList<int, IList<int>>();

			foreach (var kvp in kvps)
			{
				if (sortedList.ContainsKey(kvp.Index))
					sortedList[kvp.Index].Add(kvp.Value);
				else
					sortedList[kvp.Index] = new List<int>(new[] { kvp.Value });
			}

			return sortedList;
		}

		private int Evaluate(ExpressionInteger left, ExpressionInteger right)
		{
			return this[Index.Value];
		}

		private Bounds<int> EvaluateBounds(ExpressionInteger left, ExpressionInteger right)
		{
			var elements = Elements();

			return new Bounds<int>(elements.Min(), elements.Max());
		}

		private ConstraintOperationResult Propagator(ExpressionInteger left, ExpressionInteger right, Bounds<int> enforce)
		{
			var result = ConstraintOperationResult.Undecided;

			var sortedElements = SortedElements();

			if (enforce.UpperBound < sortedElements.First().Key || enforce.LowerBound > sortedElements.Last().Key)
				return ConstraintOperationResult.Violated;

			var remove = sortedElements.
				TakeWhile(v => v.Key < enforce.LowerBound).
				Select(v => v.Value).
				Concat(sortedElements.
					Reverse().
					TakeWhile(v => v.Key > enforce.UpperBound).
					Select(v => v.Value)).
				SelectMany(i => i.ToList()).
				ToList();

			if (remove.Any())
			{
				result = ConstraintOperationResult.Propagated;

				foreach (var value in remove)
				{
					Index.Remove(value, out DomainOperationResult domainOperation);

					if (domainOperation == DomainOperationResult.EmptyDomain)
						return ConstraintOperationResult.Violated;
				}

				left.Bounds = EvaluateBounds(left, null);
			}

			return result;
		}
	}
}
