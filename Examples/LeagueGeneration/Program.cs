/*
  Copyright © Iain McDonald 2010-2020
  
  This file is part of Decider.
*/
using System;
using System.Collections.Generic;
using System.Linq;

namespace Decider.Example.LeagueGeneration
{
	public class Program
	{
		static void Main(string[] args)
		{
            var leagueGeneration = new LeagueGeneration((args.Length >= 1) ? Int32.Parse(args[0]) : 20);
            leagueGeneration.Search();
            leagueGeneration.GenerateFixtures();

            for (var i = 0; i < leagueGeneration.FixtureWeeks.Length; ++i)
			{
				for (var j = 0; j < leagueGeneration.FixtureWeeks[i].Length; ++j)
					Console.Write(string.Format("{0,2}", leagueGeneration.FixtureWeeks[i][j]) + " ");

				Console.WriteLine();
			}

			Console.WriteLine();
			Console.WriteLine("Runtime:\t{0}\nBacktracks:\t{1}", leagueGeneration.State.Runtime, leagueGeneration.State.Backtracks);
			Console.WriteLine("Solutions:\t{0}", leagueGeneration.State.NumberOfSolutions);
        }
    }
}