using System;
using System.Collections.Generic;
using System.Linq;

using Decider.Csp.BaseTypes;
using Decider.Csp.Integer;

namespace Decider.Example.PhaseLockedLoop
{
	public static class PhaseLockedLoop
	{
		static void Main(string[] args)
		{
			var refF = 26;
			var pllOut = 142;

			if (args.Length == 2)
			{
				refF = Int32.Parse(args[0]);
				pllOut = Int32.Parse(args[1]);
			}

			//	Model
			var f1 = new VariableInteger("f1", 1, 256);
			var f2 = new VariableInteger("f2", 1, 256);
			var r1 = new VariableInteger("r1", 1, 64);
			var r2 = new VariableInteger("r2", 1, 64);
			var q1 = new VariableInteger("q1", Enumerable.Range(1, 8).Select(i => (int) Math.Pow(2, i)).ToList());
			var q2 = new VariableInteger("q2", Enumerable.Range(1, 8).Select(i => (int) Math.Pow(2, i)).ToList());


			//	Constraints
			const int divrMin = 10;
			const int divrMax = 1000;
			const int vcoMin = 1600;
			const int vcoMax = 3200;
			const int refMin = 25;
			const int refMax = 600;

			var constraints = new List<IConstraint>
				{
					new ConstraintInteger(pllOut * r1 * r2 * q1 * q2 == refF * f1 * f2),
					new ConstraintInteger(refF >= divrMin * r1),
					new ConstraintInteger(refF <= divrMax * r1),
					new ConstraintInteger(refF * f1 >= vcoMin * r1),
					new ConstraintInteger(refF * f1 <= vcoMax * r1),
					new ConstraintInteger(refF * f1 >= refMin * r1 * q1),
					new ConstraintInteger(refF * f1 <= refMax * r1 * q1),
					new ConstraintInteger(refF * f1 >= divrMin * r2 * r1 * q1),
					new ConstraintInteger(refF * f1 <= divrMax * r2 * r1 * q1),
					new ConstraintInteger(refF * f1 * f2 >= vcoMin * r2 * r1 * q1),
					new ConstraintInteger(refF * f1 * f2 <= vcoMax * r2 * r1 * q1),
				};


			//	Search
			IState<int> state = new StateInteger(new[] { f1, f2, r1, r2, q1, q2 }, constraints);

			StateOperationResult searchResult;
			state.StartSearch(out searchResult);

			Console.WriteLine("Runtime:\t{0}\nBacktracks:\t{1}\n", state.Runtime, state.Backtracks);

			if (searchResult == StateOperationResult.Solved)
			{
				var tmp = (double) (refF * f1.Value) / (r1.Value * q1.Value);

				Console.WriteLine("refF: {0}\tpllOut: {1}", refF, pllOut);
				Console.WriteLine();
				Console.WriteLine("tmp == (refF * f1) / (r1 * q1)");
				Console.WriteLine("{0} == ({1} * {2}) / ({3} * {4})", tmp, refF, f1, r1, q1);
				Console.WriteLine();
				Console.WriteLine("pllout == (tmp * f2) / (r2 * q2)");
				Console.WriteLine("{0} == ({1} * {2}) / ({3} * {4})", pllOut, tmp, f2, r2, q2);
				Console.WriteLine();
			}
			else
			{
				Console.WriteLine("No solution found.");
				Console.WriteLine();
			}

			Console.ReadKey();
		}
	}
}
