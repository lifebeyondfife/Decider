﻿/*
  Copyright © Iain McDonald 2010-2021
  
  This file is part of Decider.
*/
using System;
using System.Collections.Generic;

using Decider.Csp.BaseTypes;
using Decider.Csp.Integer;
using Decider.Csp.Global;

namespace Decider.Example.SendMoreMoney
{
	public static class SendMoreMoney
	{
		public static void Main()
		{
			var s = new VariableInteger("s", 0, 9);
			var e = new VariableInteger("e", 0, 9);
			var n = new VariableInteger("n", 0, 9);
			var d = new VariableInteger("d", 0, 9);
			var m = new VariableInteger("m", 1, 9);
			var o = new VariableInteger("o", 0, 9);
			var r = new VariableInteger("r", 0, 9);
			var y = new VariableInteger("y", 0, 9);
			var c0 = new VariableInteger("c0", 0, 1);
			var c1 = new VariableInteger("c1", 0, 1);
			var c2 = new VariableInteger("c2", 0, 1);
			var c3 = new VariableInteger("c3", 0, 1);

			var constraints = new List<IConstraint>
				{
					new AllDifferentInteger(new [] { s, e, n, d, m, o, r, y }),
					new ConstraintInteger(d + e == (10 * c0) + y),
					new ConstraintInteger(n + r + c0 == (10 * c1) + e),
					new ConstraintInteger(e + o + c1 == (10 * c2) + n),
					new ConstraintInteger(s + m + c2 == (10 * c3) + o),
					new ConstraintInteger(c3 == m)
				};

			var variables = new [] { c0, c1, c2, c3, s, e, n, d, m, o, r, y };
			IState<int> state = new StateInteger(variables, constraints);

			state.StartSearch(out StateOperationResult searchResult);

			Console.WriteLine("Runtime:\t{0}\nBacktracks:\t{1}\n", state.Runtime, state.Backtracks);

			Console.WriteLine("    {0} {1} {2} {3} ", s, e, n, d);
			Console.WriteLine("  + {0} {1} {2} {3} ", m, o, r, e);
			Console.WriteLine("  ---------");
			Console.WriteLine("  {0} {1} {2} {3} {4} ", m, o, n, e, y);
		}
	}
}
