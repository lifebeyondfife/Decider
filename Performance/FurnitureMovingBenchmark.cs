/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Decider.Csp.BaseTypes;
using Decider.Csp.Global;
using Decider.Csp.Integer;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Decider.Performance;

/// <summary>
/// Benchmark for the Furniture Moving Intervals problem using Decider.
/// Solves the same problem as OrToolsFurnitureMovingBenchmark for direct comparison.
///
/// Problem description from Marriott & Stukey: 'Programming with constraints', page 112f
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BacktracksConfig))]
public class FurnitureMovingBenchmark
{
	private const int N = 7;
	private static readonly IList<int> Durations = new List<int> { 30, 10, 15, 15, 20, 25, 12 };
	private static readonly IList<int> Demands = new List<int> { 3, 1, 3, 2, 4, 2, 3 };
	private const int Capacity = 4;
	private const int UpperLimit = 160;

	private static IState<int> SolveModel()
	{
		var starts = new List<IVariable<int>>();
		foreach (var i in Enumerable.Range(0, N))
			starts.Add(new VariableInteger($"task_{i}", 0, UpperLimit - Durations[i]));

		var makespan = new VariableInteger("makespan", 0, UpperLimit);

		var constraints = new List<IConstraint>
		{
			new CumulativeInteger(starts, Durations, Demands, Capacity)
		};

		foreach (var i in Enumerable.Range(0, N))
			constraints.Add(new ConstraintInteger(makespan >= (VariableInteger) starts[i] + Durations[i]));

		var variables = new List<IVariable<int>>(starts) { makespan };
		var state = new StateInteger(variables, constraints, new DomWdegOrdering(variables, constraints), new MiddleValueOrdering());
		state.Search(makespan);

		return state;
	}

	[Benchmark]
	public void SolveFurnitureMoving()
	{
		SolveModel();
	}

	private class BacktracksConfig : ManualConfig
	{
		public BacktracksConfig()
		{
			AddColumn(new BacktracksColumn());
		}
	}

	private class BacktracksColumn : IColumn
	{
		public string Id => nameof(BacktracksColumn);
		public string ColumnName => "Backtracks";
		public bool AlwaysShow => true;
		public ColumnCategory Category => ColumnCategory.Custom;
		public int PriorityInCategory => 0;
		public bool IsNumeric => true;
		public UnitType UnitType => UnitType.Dimensionless;
		public string Legend => "Number of backtracks during search";

		public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
		{
			return SolveModel().Backtracks.ToString("N0");
		}

		public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);
		public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
		public bool IsAvailable(Summary summary) => true;
	}
}
