/*
  Copyright © Iain McDonald 2010-2022

  This file is part of Decider.
*/
using System;

namespace Decider.Example.LeagueGeneration;

public class Program
{
	public static void Main(string[] args)
	{
		var leagueGeneration = new LeagueGeneration((args.Length >= 1) ? Int32.Parse(args[0]) : 20);
		leagueGeneration.Search();
		leagueGeneration.GenerateFixtures();

		for (var i = 0; i < leagueGeneration.FixtureWeeks.Length; ++i)
		{
			for (var j = 0; j < leagueGeneration.FixtureWeeks[i].Length; ++j)
				Console.Write($"{leagueGeneration.FixtureWeeks[i][j],2} ");

			Console.WriteLine();
		}

		Console.WriteLine();
		Console.WriteLine($"Runtime:\t{leagueGeneration.State.Runtime}");
		Console.WriteLine($"Backtracks:\t{leagueGeneration.State.Backtracks}");
	}
}
