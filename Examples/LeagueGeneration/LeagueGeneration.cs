/*
  Copyright © Iain McDonald 2010-2022
  
  This file is part of Decider.
*/
using System;
using System.Collections.Generic;
using System.Linq;

using Decider.Csp.BaseTypes;
using Decider.Csp.Global;
using Decider.Csp.Integer;

namespace Decider.Example.LeagueGeneration;

public class LeagueGeneration
{
	private VariableInteger[][] Variables { get; set; }
	private IList<IConstraint> Constraints { get; set; }
	private int LeagueSize { get; set; }
	
	public IState<int> State { get; private set; }
	public int[][] FixtureWeeks { get; private set; }

	public LeagueGeneration(int leagueSize)
	{
		this.LeagueSize = leagueSize;
		this.Variables = new VariableInteger[this.LeagueSize - 1][];

		for (var i = 0; i < Variables.Length; ++i)
			this.Variables[i] = new VariableInteger[i + 1];

		for (var i = 0; i < Variables.Length; ++i)
			for (var j = 0; j < Variables[i].Length; ++j)
				this.Variables[i][j] = new VariableInteger(string.Format("{0} v {1}", i, j), 1, LeagueSize - 1);

		for (var week = 1; week < LeagueSize; ++week)
		{
			var i = week - 1;
			var j = 0;

			do
			{
				this.Variables[i][j] = new VariableInteger(string.Format("{0} v {1}", i, j), week, week);
				--i;
				++j;
			} while (i >= j);
		}

		this.Constraints = [];

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

			this.Constraints.Add(new AllDifferentInteger(allDifferentRow));
		}
	}

	public void Search()
	{
		this.State = new StateInteger(this.Variables.SelectMany(s => s.Select(a => a)), this.Constraints);
		((StateInteger)this.State).ClauseLearningEnabled = false;
		this.State.Search();
	}

	public void GenerateFixtures()
	{
		var map = Enumerable.Range(1, (this.LeagueSize - 1) * 2).
			Select((e, i) => Tuple.Create(i + 1, e)).
			ToDictionary(t => t.Item1, t => t.Item2);

		this.FixtureWeeks = new int[LeagueSize][];
		for (var i = 0; i < this.FixtureWeeks.Length; ++i)
			this.FixtureWeeks[i] = new int[this.LeagueSize];

		for (int i = 0; i < this.Variables.Length; ++i)
			for (int j = 0; j < this.Variables[i].Length; ++j)
			{
				this.FixtureWeeks[i + 1][j] = map[this.Variables[i][j].Value];
				this.FixtureWeeks[j][i + 1] = map[this.Variables[i][j].Value + this.LeagueSize - 1];
			}
	}
}
