/*
  Copyright © Iain McDonald 2010-2026

  This file is part of Decider.
*/
using System.Collections.Generic;
using Xunit;

using Decider.Csp.BaseTypes;
using Decider.Example.Rcpsp;

namespace Decider.Tests.Example;

public class RcpspTest
{
	[Fact]
	public void TestRcpspFindsSolution()
	{
		var rcpsp = new Rcpsp();
		rcpsp.Solve();

		Assert.NotNull(rcpsp.Solution);
		Assert.Equal(StateOperationResult.Solved, rcpsp.State.Search());
	}

	[Fact]
	public void TestRcpspOptimalMakespan()
	{
		var rcpsp = new Rcpsp();
		rcpsp.OptimiseMakespan();

		Assert.NotNull(rcpsp.Solution);
		Assert.True(rcpsp.Solution["9"].InstantiatedValue >= 0);
		Assert.True(rcpsp.Solution["9"].InstantiatedValue <= 30);
	}

	[Fact]
	public void TestRcpspPrecedenceConstraints()
	{
		var rcpsp = new Rcpsp();
		rcpsp.Solve();

		var durations = new List<int> { 0, 3, 2, 5, 4, 2, 3, 4, 2, 0 };

		Assert.True(rcpsp.Solution!["1"].InstantiatedValue >= rcpsp.Solution["0"].InstantiatedValue + durations[0]);
		Assert.True(rcpsp.Solution["2"].InstantiatedValue >= rcpsp.Solution["0"].InstantiatedValue + durations[0]);
		Assert.True(rcpsp.Solution["3"].InstantiatedValue >= rcpsp.Solution["1"].InstantiatedValue + durations[1]);
		Assert.True(rcpsp.Solution["4"].InstantiatedValue >= rcpsp.Solution["1"].InstantiatedValue + durations[1]);
		Assert.True(rcpsp.Solution["5"].InstantiatedValue >= rcpsp.Solution["2"].InstantiatedValue + durations[2]);
		Assert.True(rcpsp.Solution["6"].InstantiatedValue >= rcpsp.Solution["3"].InstantiatedValue + durations[3]);
		Assert.True(rcpsp.Solution["7"].InstantiatedValue >= rcpsp.Solution["4"].InstantiatedValue + durations[4]);
		Assert.True(rcpsp.Solution["8"].InstantiatedValue >= rcpsp.Solution["5"].InstantiatedValue + durations[5]);
		Assert.True(rcpsp.Solution["9"].InstantiatedValue >= rcpsp.Solution["6"].InstantiatedValue + durations[6]);
		Assert.True(rcpsp.Solution["9"].InstantiatedValue >= rcpsp.Solution["7"].InstantiatedValue + durations[7]);
		Assert.True(rcpsp.Solution["9"].InstantiatedValue >= rcpsp.Solution["8"].InstantiatedValue + durations[8]);
	}
}
