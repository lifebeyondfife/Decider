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
namespace Decider.Csp.BaseTypes
{
	public struct Bounds<T>
	{
		public T LowerBound;
		public T UpperBound;

		public Bounds(T lowerBound, T upperBound)
		{
			this.LowerBound = lowerBound;
			this.UpperBound = upperBound;
		}
	}

	public abstract class Expression<T>
	{
		public abstract T Value { get; }
		public abstract bool IsBound { get; }
		public abstract Bounds<T> GetUpdatedBounds();
		public abstract void Propagate(Bounds<T> enforceBounds, out ConstraintOperationResult result);
		public Bounds<T> Bounds;
	}
}
