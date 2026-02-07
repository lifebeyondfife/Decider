using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Google.OrTools.Sat;

namespace Decider.Performance
{
	[MemoryDiagnoser]
	[Config(typeof(OrToolsStatsConfig))]
	public class OrToolsNQueensBenchmark
	{
		[Params(8, 10, 12)]
		public int BoardSize { get; set; }

		[Benchmark]
		public void SolveNQueens()
		{
			var model = new CpModel();
			var queens = new IntVar[BoardSize];

			for (var i = 0; i < BoardSize; i++)
			{
				queens[i] = model.NewIntVar(0, BoardSize - 1, $"queen_{i}");
			}

			model.AddAllDifferent(queens);

			for (var i = 0; i < BoardSize - 1; i++)
			{
				for (var j = i + 1; j < BoardSize; j++)
				{
					model.Add(queens[i] - queens[j] != j - i);
					model.Add(queens[i] - queens[j] != i - j);
				}
			}

			var solver = new CpSolver
			{
				StringParameters = "enumerate_all_solutions:true"
			};
			var solutionCollector = new SolutionCollector();
			solver.Solve(model, solutionCollector);
		}

		private class SolutionCollector : CpSolverSolutionCallback
		{
			public override void OnSolutionCallback()
			{
			}
		}

		private class OrToolsStatsConfig : ManualConfig
		{
			public OrToolsStatsConfig()
			{
				AddColumn(new ConflictsColumn());
				AddColumn(new BranchesColumn());
			}
		}

		private class ConflictsColumn : IColumn
		{
			public string Id => nameof(ConflictsColumn);
			public string ColumnName => "Conflicts";
			public bool AlwaysShow => true;
			public ColumnCategory Category => ColumnCategory.Custom;
			public int PriorityInCategory => 0;
			public bool IsNumeric => true;
			public UnitType UnitType => UnitType.Dimensionless;
			public string Legend => "Number of conflicts (analogous to backtracks)";

			public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
			{
				var parameters = benchmarkCase.Parameters;
				if (parameters.Items.Count > 0 && parameters.Items[0].Name == "BoardSize")
				{
					var boardSize = (int)parameters.Items[0].Value;
					var model = new CpModel();
					var queens = new IntVar[boardSize];

					for (var i = 0; i < boardSize; i++)
					{
						queens[i] = model.NewIntVar(0, boardSize - 1, $"queen_{i}");
					}

					model.AddAllDifferent(queens);

					for (var i = 0; i < boardSize - 1; i++)
					{
						for (var j = i + 1; j < boardSize; j++)
						{
							model.Add(queens[i] - queens[j] != j - i);
							model.Add(queens[i] - queens[j] != i - j);
						}
					}

					var solver = new CpSolver
					{
						StringParameters = "enumerate_all_solutions:true"
					};
					var collector = new SolutionCollector();
					solver.Solve(model, collector);

					return solver.NumConflicts().ToString("N0");
				}
				return "-";
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
				var parameters = benchmarkCase.Parameters;
				if (parameters.Items.Count > 0 && parameters.Items[0].Name == "BoardSize")
				{
					var boardSize = (int)parameters.Items[0].Value;
					var model = new CpModel();
					var queens = new IntVar[boardSize];

					for (var i = 0; i < boardSize; i++)
					{
						queens[i] = model.NewIntVar(0, boardSize - 1, $"queen_{i}");
					}

					model.AddAllDifferent(queens);

					for (var i = 0; i < boardSize - 1; i++)
					{
						for (var j = i + 1; j < boardSize; j++)
						{
							model.Add(queens[i] - queens[j] != j - i);
							model.Add(queens[i] - queens[j] != i - j);
						}
					}

					var solver = new CpSolver
					{
						StringParameters = "enumerate_all_solutions:true"
					};
					var collector = new SolutionCollector();
					solver.Solve(model, collector);

					return solver.NumBranches().ToString("N0");
				}
				return "-";
			}

			public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);
			public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
			public bool IsAvailable(Summary summary) => true;
		}
	}
}
