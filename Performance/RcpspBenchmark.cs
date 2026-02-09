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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Decider.Performance;

[MemoryDiagnoser]
[Config(typeof(BacktracksConfig))]
public class RcpspBenchmark
{
	private static string GetDataFilePath(string fileName)
	{
		var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		return Path.Combine(assemblyLocation, "Data", fileName);
	}

	[Benchmark]
	public void SolveRcpspJ30Instance()
	{
		SolvePspLibInstance(GetDataFilePath("j3010_1.sm"));
	}

	private IState<int> SolvePspLibInstance(string instanceFile)
	{
		var instance = PspLibParser.Parse(instanceFile);

		var starts = new List<VariableInteger>();
		foreach (var i in Enumerable.Range(0, instance.JobCount))
			starts.Add(new VariableInteger(i.ToString(CultureInfo.CurrentCulture), 0, instance.Horizon));

		var constraints = new List<IConstraint>();

		foreach (var r in Enumerable.Range(0, instance.ResourceCount))
		{
			var demands = new List<int>();
			foreach (var jobDemands in instance.ResourceDemands)
				demands.Add(jobDemands[r]);

			constraints.Add(new CumulativeInteger(starts, instance.Durations, demands, instance.ResourceCapacities[r]));
		}

		constraints.Add(new ConstraintInteger(starts[0] == 0));

		foreach (var j in Enumerable.Range(0, instance.JobCount))
		{
			foreach (var successor in instance.Successors[j])
			{
				constraints.Add(new ConstraintInteger(
					starts[successor] >= starts[j] + instance.Durations[j]));
			}
		}

		var state = new StateInteger(starts, constraints);
		state.Search();
		return state;
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
			var state = new RcpspBenchmark().SolvePspLibInstance(GetDataFilePath("j3010_1.sm"));
			return state.Backtracks.ToString("N0");
		}

		public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);
		public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
		public bool IsAvailable(Summary summary) => true;
	}
}
