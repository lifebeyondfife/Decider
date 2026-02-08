/*
  Copyright © Iain McDonald 2010-2022

  This file is part of Decider.
*/
using System;
using System.Globalization;

namespace Decider.Example.NQueens;

public class Program
{
	public static void Main(string[] args)
	{
		var nQueens = new NQueens((args.Length >= 1) ? Int32.Parse(args[0]) : 8);
		nQueens.SearchAllSolutions();

		foreach (var solution in nQueens.Solutions)
		{
			for (var i = 0; i < nQueens.State.Variables.Count; ++i)
			{
				for (var j = 0; j < nQueens.State.Variables.Count; ++j)
					Console.Write(solution[i.ToString(CultureInfo.CurrentCulture)].InstantiatedValue == j ? "Q " : ". ");

				Console.WriteLine();
			}

			Console.WriteLine();
		}

		Console.WriteLine($"Runtime:\t{nQueens.State.Runtime}");
		Console.WriteLine($"Backtracks:\t{nQueens.State.Backtracks}");
		Console.WriteLine($"Solutions:\t{nQueens.State.Solutions.Count}");
	}
}
