/*
  Copyright © Iain McDonald 2010-2026

  This file is part of Decider.
*/
using System;
using System.Globalization;

namespace Decider.Example.NQueens;

public class Program
{
	public static void Main(string[] args)
	{
		var n = (args.Length >= 1) ? Int32.Parse(args[0]) : 13;
		var nQueens = new NQueens(n);

		nQueens.SearchAllSolutions();
		Console.WriteLine();

		if (n <= 8)
		{
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
		}

		Console.WriteLine($"Runtime:\t{nQueens.State.Runtime}");
		Console.WriteLine($"Backtracks:\t{nQueens.State.Backtracks}");
		Console.WriteLine($"Solutions:\t{nQueens.State.Solutions.Count}");
	}
}
