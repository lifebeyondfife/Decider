/*
  Copyright © Iain McDonald 2010-2021
  
  This file is part of Decider.
*/
using System;
using System.Collections.Generic;

using Decider.Csp.BaseTypes;
using Decider.Csp.Integer;
using Decider.Csp.Global;

namespace Decider.Example.Optimisation
{
	public static class Optimisation
	{
		public static void Main()
		{
			var a = new VariableInteger("a", 0, 9);
			var b = new VariableInteger("b", 0, 9);
			var c = new VariableInteger("c", 0, 9);
			var d = new VariableInteger("d", 0, 9);
			var e = new VariableInteger("e", 0, 9);
			var f = new VariableInteger("f", 0, 9);
			var g = new VariableInteger("g", 0, 9);
			var h = new VariableInteger("h", 0, 9);
			var optimise = new VariableInteger("optimise", 0, 72);

			var array = new ConstrainedArray(new int[] { 60, 52, 52, 62, 35, 73, 47, 20, 87, 27 });

			var constraints = new List<IConstraint>
				{
					new AllDifferentInteger(new [] { a, b, c, d }),
					new AllDifferentInteger(new [] { e, f, g, h }),
					new ConstraintInteger(a + b < 10),
					new ConstraintInteger(c + d > 15),
					new ConstraintInteger(h > e),
					new ConstraintInteger(array[a] < 40),
					new ConstraintInteger(optimise == a + b + c + d + e + f + g + h)
				};

			var variables = new[] { a, b, c, d, e, f, g, h, optimise };
			IState<int> state = new StateInteger(variables, constraints);

			state.StartSearch(out StateOperationResult searchResult, optimise, out IDictionary<string, IVariable<int>> solution, 20);

			Console.WriteLine("a: {0}", solution["a"]);
			Console.WriteLine("b: {0}", solution["b"]);
			Console.WriteLine("c: {0}", solution["c"]);
			Console.WriteLine("d: {0}", solution["d"]);
			Console.WriteLine("e: {0}", solution["e"]);
			Console.WriteLine("f: {0}", solution["f"]);
			Console.WriteLine("g: {0}", solution["g"]);
			Console.WriteLine("h: {0}\n", solution["h"]);

			Console.WriteLine("Optimised Variable: {0}\n", solution["optimise"]);

			Console.WriteLine("Runtime:\t{0}\nBacktracks:\t{1}\n", state.Runtime, state.Backtracks);
		}
	}
}
