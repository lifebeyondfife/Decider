/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Decider.Example.NQueens;

namespace Decider.Performance;

[MemoryDiagnoser]
[Config(typeof(BacktracksConfig))]
public class NQueensBenchmark
{
	[Params(8, 10, 12)]
	public int BoardSize { get; set; }

	[Benchmark]
	public void SolveNQueens()
	{
		var nQueens = new NQueens(BoardSize);
		nQueens.SearchAllSolutions(false);
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
			var parameters = benchmarkCase.Parameters;
			if (parameters.Items.Count > 0 && parameters.Items[0].Name == "BoardSize")
			{
				var boardSize = (int)parameters.Items[0].Value;
				var nQueens = new NQueens(boardSize);
				nQueens.SearchAllSolutions(false);
				return nQueens.State.Backtracks.ToString("N0");
			}
			return "-";
		}

		public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);
		public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
		public bool IsAvailable(Summary summary) => true;
	}
}
