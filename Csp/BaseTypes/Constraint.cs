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

namespace Decider.Csp.BaseTypes
{
	[Flags]
	public enum ConstraintOperationResult
	{
		Satisfied = 0x1,
		Violated = 0x2,
		Undecided = 0x4,
		Propagated = 0x8
	}

	public interface IConstraint
	{
		void Check(out ConstraintOperationResult result);
		void Propagate(out ConstraintOperationResult result);
		bool StateChanged();
	}
}
