using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Decider.Csp.BaseTypes;
using Decider.Csp.Global;
using Decider.Csp.Integer;

namespace Decider.Example.LeagueGeneration
{
	class LeagueGeneration
	{
		static void Main(string[] args)
		{
			#region Model

			var leagueSize = (args.Length >= 1) ? Int32.Parse(args[0]) : 20;

			var variables = new VariableInteger[leagueSize - 1][];

			for (var i = 0; i < variables.Length; ++i)
				variables[i] = new VariableInteger[i + 1];

			for (var i = 0; i < variables.Length; ++i)
				for (var j = 0; j < variables[i].Length; ++j)
					variables[i][j] = new VariableInteger(string.Format("{0} v {1}", i, j), 1, leagueSize - 1);

			for (var week = 1; week < leagueSize; ++week)
			{
				var i = week - 1;
				var j = 0;

				do
				{
					variables[i][j] = new VariableInteger(string.Format("{0} v {1}", i, j), week, week);
					--i;
					++j;
				} while (i >= j);
			}

			#endregion

			#region Constraints

			var constraints = new List<IConstraint>();

			for (int row = -1; row < leagueSize - 1; ++row)
			{
				var j = 0;
				var i = row;

				var allDifferentRow = new List<VariableInteger>();

				while (i >= j)
					allDifferentRow.Add(variables[i][j++]);

				++i;

				while (i < leagueSize - 1)
					allDifferentRow.Add(variables[i++][j]);

				constraints.Add(new AllDifferentInteger(allDifferentRow));
			}

			#endregion

			#region Search

			IState<int> state = new StateInteger(variables.SelectMany(s => s.Select(a => a)), constraints);

			StateOperationResult searchResult;
			state.StartSearch(out searchResult);

			for (var i = 0; i < variables.Length; ++i)
			{
				for (var j = 0; j < variables[i].Length; ++j)
					Console.Write(string.Format("{0,2}", variables[i][j]) + " ");

				Console.WriteLine();
			}

			Console.WriteLine();
			Console.WriteLine("Runtime:\t{0}\nBacktracks:\t{1}", state.Runtime, state.Backtracks);
			Console.WriteLine("Solutions:\t{0}", state.NumberOfSolutions);

			Console.ReadKey();

			#endregion
		}
	}
}
