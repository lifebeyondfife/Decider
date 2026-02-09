/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System;
using System.Globalization;
using System.Linq;

namespace Decider.Example.Rcpsp;

public class Program
{
	public static void Main(string[] args)
	{
		var rcpsp = new Rcpsp();

		Console.WriteLine("Solving RCPSP instance...");
		rcpsp.OptimiseMakespan();
		Console.WriteLine();

		Console.WriteLine("Optimal Schedule:");
		foreach (var index in Enumerable.Range(0, rcpsp.State.Variables.Count))
		{
			var taskId = index.ToString(CultureInfo.CurrentCulture);
			var startTime = rcpsp.Solution![taskId].InstantiatedValue;
			Console.WriteLine($"Task {taskId}: starts at time {startTime}");
		}

		Console.WriteLine();
		Console.WriteLine($"Makespan:\t{rcpsp.Solution!["9"].InstantiatedValue}");
		Console.WriteLine($"Runtime:\t{rcpsp.State.Runtime}");
		Console.WriteLine($"Backtracks:\t{rcpsp.State.Backtracks}");
	}
}
