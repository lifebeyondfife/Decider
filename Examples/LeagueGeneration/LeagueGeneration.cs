/*
  Copyright © Iain McDonald 2010-2020
  
  This file is part of Decider.
*/
using System;
using System.Collections.Generic;
using System.Linq;

using Decider.Csp.BaseTypes;
using Decider.Csp.Global;
using Decider.Csp.Integer;

namespace Decider.Example.LeagueGeneration
{
	public class LeagueGeneration
	{
		private VariableInteger[][] Variables { get; set; }
		private IList<IConstraint> Constraints { get; set; }
		private int LeagueSize { get; set; }
		
		public IState<int> State { get; private set; }
		public int[][] FixtureWeeks { get; private set; }

		public LeagueGeneration(int leagueSize)
		{
			LeagueSize = leagueSize;
			Variables = new VariableInteger[LeagueSize - 1][];

			for (var i = 0; i < Variables.Length; ++i)
				Variables[i] = new VariableInteger[i + 1];

			for (var i = 0; i < Variables.Length; ++i)
				for (var j = 0; j < Variables[i].Length; ++j)
					Variables[i][j] = new VariableInteger(string.Format("{0} v {1}", i, j), 1, LeagueSize - 1);

			for (var week = 1; week < LeagueSize; ++week)
			{
				var i = week - 1;
				var j = 0;

				do
				{
					Variables[i][j] = new VariableInteger(string.Format("{0} v {1}", i, j), week, week);
					--i;
					++j;
				} while (i >= j);
			}

			Constraints = new List<IConstraint>();

			for (int row = -1; row < LeagueSize - 1; ++row)
			{
				var j = 0;
				var i = row;

				var allDifferentRow = new List<VariableInteger>();

				while (i >= j)
					allDifferentRow.Add(Variables[i][j++]);

				++i;

				while (i < LeagueSize - 1)
					allDifferentRow.Add(Variables[i++][j]);

				Constraints.Add(new AllDifferentInteger(allDifferentRow));
			}
		}

		public void Search()
		{
			State = new StateInteger(Variables.SelectMany(s => s.Select(a => a)), Constraints);

			StateOperationResult searchResult;
			State.StartSearch(out searchResult);
		}

		public void GenerateFixtures()
		{
			var map = Enumerable.Range(1, (LeagueSize - 1) * 2).
				Select((e, i) => Tuple.Create(i + 1, e)).
				ToDictionary(t => t.Item1, t => t.Item2);

			FixtureWeeks = new int[LeagueSize][];
			for (var i = 0; i < FixtureWeeks.Length; ++i)
				FixtureWeeks[i] = new int[LeagueSize];

			for (int i = 0; i < Variables.Length; ++i)
				for (int j = 0; j < Variables[i].Length; ++j)
				{
					FixtureWeeks[i + 1][j] = map[Variables[i][j].Value];
					FixtureWeeks[j][i + 1] = map[Variables[i][j].Value + LeagueSize - 1];
				}
		}
	}
}
