/*
  Copyright © Iain McDonald 2010-2020
  
  This file is part of Decider.
*/
using System;
using System.Collections.Generic;
using System.Globalization;

using Decider.Csp.BaseTypes;
using Decider.Csp.Global;
using Decider.Csp.Integer;

namespace Decider.Example.NQueens
{
	class NQueens
	{
		static void Main(string[] args)
		{
			var numberOfQueens = (args.Length >= 1) ? Int32.Parse(args[0]) : 8;

			// Model
			var variables = new VariableInteger[numberOfQueens];
			for (var i = 0; i < variables.Length; ++i)
				variables[i] = new VariableInteger(i.ToString(CultureInfo.CurrentCulture), 0, numberOfQueens - 1);

			//	Constraints
			var constraints = new List<IConstraint> { new AllDifferentInteger(variables) };
			for (var i=0; i < variables.Length - 1; ++i)
				for (var j = i + 1; j < variables.Length; ++j)
				{
					constraints.Add(new ConstraintInteger(variables[i] - variables[j] != j - i));
					constraints.Add(new ConstraintInteger(variables[i] - variables[j] != i - j));
				}

			//	Search
			IState<int> state = new StateInteger(variables, constraints);

			state.StartSearch(out StateOperationResult searchResult, out IList<IDictionary<string, IVariable<int>>> solutions);

			foreach (var solution in solutions)
			{
				for (var i = 0; i < variables.Length; ++i)
				{
					for (var j = 0; j < variables.Length; ++j)
						Console.Write(solution[i.ToString(CultureInfo.CurrentCulture)].InstantiatedValue == j ? "Q" : ".");

					Console.WriteLine();
				}

				Console.WriteLine();
			}

			Console.WriteLine("Runtime:\t{0}\nBacktracks:\t{1}", state.Runtime, state.Backtracks);
			Console.WriteLine("Solutions:\t{0}", state.NumberOfSolutions);
		}
	}
}
