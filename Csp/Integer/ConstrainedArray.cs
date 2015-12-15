/*
  Copyright © Iain McDonald 2010-2015
  
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
using Decider.Csp.Integer;
using System.Collections.Generic;
using System.Linq;

namespace Decider.Csp.BaseTypes
{
	public class ConstrainedArray : List<int>
	{
		private VariableInteger Index { get; set; }

		public ExpressionInteger this[VariableInteger index]
		{
			get
			{
				Index = index;

				return new ExpressionInteger(GetVariableInteger(), this.Evaluate, this.EvaluateBounds, this.Propagator);
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

		private SortedList<int, int> SortedElements()
		{
			return new SortedList<int, int>(Enumerable.Range(Index.Domain.LowerBound, Index.Domain.UpperBound - Index.Domain.LowerBound + 1).
				Where(i => Index.Domain.Contains(i)).
				Select(i => new { Index = i, Value = this[i] }).
				ToDictionary(i => i.Value, i => i.Index));
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
				ToList();

			if (remove.Any())
			{
				result = ConstraintOperationResult.Propagated;

				var domainOperation = default(DomainOperationResult);

				foreach (var value in remove)
				{
					Index.Domain.Remove(value, out domainOperation);

					if (domainOperation == DomainOperationResult.EmptyDomain)
						return ConstraintOperationResult.Violated;
				}

				left.Bounds = EvaluateBounds(left, null);
			}

			return result;
		}
	}
}
