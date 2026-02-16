/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Google.OrTools.ConstraintSolver;

namespace Decider.Performance;

/// <summary>
/// Benchmark for the Furniture Moving Intervals problem using Google OR-Tools.
/// Based on: https://github.com/google/or-tools/blob/stable/examples/dotnet/furniture_moving_intervals.cs
///
/// Problem description from Marriott & Stukey: 'Programming with constraints', page 112f
/// A scheduling problem where furniture needs to be moved with constraints on:
/// - Task durations
/// - Worker demand per task
/// - Total available workers
/// </summary>
[MemoryDiagnoser]
[Config(typeof(OrToolsConstraintSolverConfig))]
public class OrToolsFurnitureMovingBenchmark
{
	private const int N = 7;
	private static readonly int[] Durations = { 30, 10, 15, 15, 20, 25, 12 };
	private static readonly int[] Demand = { 3, 1, 3, 2, 4, 2, 3 };
	private const int Capacity = 4;
	private const int UpperLimit = 160;

	private static Solver SolveModel()
	{
		var solver = new Solver("FurnitureMovingIntervals");

		var tasks = new IntervalVar[N];
		var startVars = new IntVar[N];
		for (var i = 0; i < N; ++i)
		{
			tasks[i] = solver.MakeFixedDurationIntervalVar(
				0,
				UpperLimit - Durations[i],
				Durations[i],
				false,
				"task_" + i);
			startVars[i] = tasks[i].StartExpr().Var();
		}

		var ends = new IntVar[N];
		for (var i = 0; i < N; ++i)
		{
			ends[i] = tasks[i].EndExpr().Var();
		}
		var endTime = ends.Max().VarWithName("end_time");

		solver.Add(tasks.Cumulative(Demand, Capacity, "workers"));

		var obj = endTime.Minimize(1);

		var db = solver.MakePhase(startVars, Solver.CHOOSE_MIN_SIZE, Solver.CHOOSE_MIN_SIZE_LOWEST_MIN);
		solver.NewSearch(db, obj);

		while (solver.NextSolution())
		{
		}

		solver.EndSearch();

		return solver;
	}

	[Benchmark]
	public void SolveFurnitureMoving()
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
