/*
  Copyright © Iain McDonald 2010-2017
  
  This file is part of Decider.
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
