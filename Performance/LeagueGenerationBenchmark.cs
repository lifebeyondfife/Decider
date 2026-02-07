using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Decider.Example.LeagueGeneration;

namespace Decider.Performance
{
	[MemoryDiagnoser]
	[Config(typeof(BacktracksConfig))]
	public class LeagueGenerationBenchmark
	{
		private const int DefaultLeagueSize = 20;

		[Benchmark]
		public void SolveLeagueGeneration()
		{
			var league = new LeagueGeneration(DefaultLeagueSize);
			league.Search();
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
				var instance = benchmarkCase.Descriptor.WorkloadMethod.DeclaringType;
				if (instance == typeof(LeagueGenerationBenchmark))
				{
					var league = new LeagueGeneration(DefaultLeagueSize);
					league.Search();
					return league.State.Backtracks.ToString("N0");
				}
				return "-";
			}

			public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);
			public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
			public bool IsAvailable(Summary summary) => true;
		}
	}
}
