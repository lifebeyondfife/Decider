/*
  Copyright © Iain McDonald 2010-2026

  This file is part of Decider.
*/
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

using Decider.Example.Rcpsp;

namespace Decider.Tests.Example;

public class RcpspTest
{
	private static string GetDataFilePath()
	{
		var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
		return Path.Combine(assemblyDir, "Data", "j3010_1.sm");
	}

	[Fact]
	public void TestRcpspFindsSolution()
	{
		var rcpsp = new Rcpsp(GetDataFilePath());
		rcpsp.OptimiseMakespan();

		Assert.NotNull(rcpsp.Solution);
	}

	[Fact]
	public void TestRcpspOptimalMakespan()
	{
		var rcpsp = new Rcpsp(GetDataFilePath());
		rcpsp.OptimiseMakespan();

		var sinkId = rcpsp.SinkTaskIndex.ToString(CultureInfo.CurrentCulture);
		var makespan = rcpsp.Solution![sinkId].InstantiatedValue;
		Assert.True(makespan >= 41, $"Makespan {makespan} is below the critical path length");
		Assert.True(makespan <= 50, $"Makespan {makespan} exceeds expected optimal range");
	}

	[Fact]
	public void TestRcpspPrecedenceConstraints()
	{
		var rcpsp = new Rcpsp(GetDataFilePath());
		rcpsp.OptimiseMakespan();

		var instance = PspLibParser.Parse(GetDataFilePath());

		foreach (var j in Enumerable.Range(0, instance.JobCount))
		{
			var jId = j.ToString(CultureInfo.CurrentCulture);
			foreach (var successor in instance.Successors[j])
			{
				var sId = successor.ToString(CultureInfo.CurrentCulture);
				Assert.True(
					rcpsp.Solution![sId].InstantiatedValue >=
					rcpsp.Solution[jId].InstantiatedValue + instance.Durations[j],
					$"Precedence violated: job {successor} starts before job {j} completes");
			}
		}
	}
}
