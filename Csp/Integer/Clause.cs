/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using Decider.Csp.BaseTypes;

namespace Decider.Csp.Integer;

internal class Clause
{
	internal BoundReason[] Literals { get; set; }
	internal int Watch1 { get; set; }
	internal int Watch2 { get; set; }
	internal double Activity { get; set; }

	internal Clause(BoundReason[] literals)
	{
		this.Literals = literals;
		this.Activity = 0.0;
		this.Watch1 = 0;
		this.Watch2 = literals.Length > 1 ? 1 : 0;
	}

	internal static bool IsLiteralFalsified(BoundReason literal, IVariable<int> variable)
	{
		if (literal.IsLowerBound)
			return variable.Domain.UpperBound < literal.BoundValue;

		return variable.Domain.LowerBound > literal.BoundValue;
	}

	internal static bool IsLiteralSatisfied(BoundReason literal, IVariable<int> variable)
	{
		if (literal.IsLowerBound)
			return variable.Domain.LowerBound >= literal.BoundValue;

		return variable.Domain.UpperBound <= literal.BoundValue;
	}
}
