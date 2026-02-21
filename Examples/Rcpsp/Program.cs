/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace Decider.Example.Rcpsp;

public class Program
{
	public static void Main(string[] args)
	{
		var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
		var instanceFile = Path.Combine(assemblyDir, "Data", "j3010_1.sm");

		var rcpsp = new Rcpsp(instanceFile);

		Console.WriteLine($"RCPSP — PSPLib j3010_1");
		Console.WriteLine($"  {rcpsp.TaskCount} jobs");
		Console.WriteLine();
		Console.WriteLine("Solving for minimum makespan...");

		rcpsp.OptimiseMakespan();

		var sinkId = rcpsp.SinkTaskIndex.ToString(CultureInfo.CurrentCulture);
		Console.WriteLine();
		Console.WriteLine($"Optimal Makespan:  {rcpsp.Solution[sinkId].InstantiatedValue}");
		Console.WriteLine($"Runtime:           {rcpsp.State.Runtime}");
		Console.WriteLine($"Backtracks:        {rcpsp.State.Backtracks}");
	}
}
