/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Decider.Example.Rcpsp;
using Google.OrTools.ConstraintSolver;
using System.IO;
using System.Reflection;

namespace Decider.Performance;

[MemoryDiagnoser]
[Config(typeof(OrToolsConstraintSolverConfig))]
public class OrToolsRcpspBenchmark
{
	private const int HorizonMultiplier = 10;

	private static string GetDataFilePath(string fileName)
	{
		var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		return Path.Combine(assemblyLocation, "Data", fileName);
	}

	private static Solver SolveModel()
	{
		var instance = PspLibParser.Parse(GetDataFilePath("j3010_1.sm"));
		var scaledHorizon = instance.Horizon * HorizonMultiplier;

		var solver = new Solver("RcpspJ30");

		var tasks = new IntervalVar[instance.JobCount];
		var startVars = new IntVar[instance.JobCount];

		for (var i = 0; i < instance.JobCount; ++i)
		{
			tasks[i] = solver.MakeFixedDurationIntervalVar(
				0,
				scaledHorizon - instance.Durations[i],
				instance.Durations[i],
				false,
				"job_" + i);
			startVars[i] = tasks[i].StartExpr().Var();
		}

		solver.Add(startVars[0] == 0);

		for (var r = 0; r < instance.ResourceCount; ++r)
		{
			var demands = new int[instance.JobCount];
			for (var i = 0; i < instance.JobCount; ++i)
				demands[i] = instance.ResourceDemands[i][r];

			solver.Add(tasks.Cumulative(demands, instance.ResourceCapacities[r], $"resource_{r}"));
		}

		for (var j = 0; j < instance.JobCount; ++j)
		{
			foreach (var successor in instance.Successors[j])
				solver.Add(tasks[successor].StartsAfterEnd(tasks[j]));
		}

		var obj = startVars[instance.JobCount - 1].Minimize(1);

		var db = solver.MakePhase(startVars, Solver.CHOOSE_MIN_SIZE, Solver.ASSIGN_MIN_VALUE);
		solver.NewSearch(db, obj);

		while (solver.NextSolution())
		{
		}

		solver.EndSearch();
		return solver;
	}

	[Benchmark]
	public void SolveRcpspJ30Instance()
	{
		SolveModel();
	}

	private class OrToolsConstraintSolverConfig : ManualConfig
	{
		public OrToolsConstraintSolverConfig()
		{
			AddColumn(new FailuresColumn());
			AddColumn(new BranchesColumn());
		}
	}

	private class FailuresColumn : IColumn
	{
		public string Id => nameof(FailuresColumn);
		public string ColumnName => "Failures";
		public bool AlwaysShow => true;
		public ColumnCategory Category => ColumnCategory.Custom;
		public int PriorityInCategory => 0;
		public bool IsNumeric => true;
		public UnitType UnitType => UnitType.Dimensionless;
		public string Legend => "Number of failures (analogous to backtracks)";

		public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
		{
			return SolveModel().Failures().ToString("N0");
		}

		public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);
		public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
		public bool IsAvailable(Summary summary) => true;
	}

	private class BranchesColumn : IColumn
	{
		public string Id => nameof(BranchesColumn);
		public string ColumnName => "Branches";
		public bool AlwaysShow => true;
		public ColumnCategory Category => ColumnCategory.Custom;
		public int PriorityInCategory => 1;
		public bool IsNumeric => true;
		public UnitType UnitType => UnitType.Dimensionless;
		public string Legend => "Number of search branches explored";

		public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
		{
			return SolveModel().Branches().ToString("N0");
		}

		public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);
		public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
		public bool IsAvailable(Summary summary) => true;
	}
}
