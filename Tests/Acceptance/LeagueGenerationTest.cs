/*
  Copyright © Iain McDonald 2010-2022

  This file is part of Decider.
*/
using Xunit;

using Decider.Example.LeagueGeneration;

namespace Decider.Tests.Example;

public class LeagueGenerationTest
{
    [Fact]
    public void TestGenerating10()
    {
        var leagueGeneration = new LeagueGeneration(10);
        leagueGeneration.Search();
        leagueGeneration.GenerateFixtures();

        Assert.Equal(10, leagueGeneration.FixtureWeeks[0][1]);
        Assert.Equal(11, leagueGeneration.FixtureWeeks[0][2]);
        Assert.Equal(12, leagueGeneration.FixtureWeeks[0][3]);

        Assert.Equal(10, leagueGeneration.FixtureWeeks[5][6]);
        Assert.Equal(11, leagueGeneration.FixtureWeeks[5][7]);
        Assert.Equal(12, leagueGeneration.FixtureWeeks[5][8]);

        Assert.Equal(0, leagueGeneration.State.Backtracks);
        Assert.Single(leagueGeneration.State.Solutions);
    }
    
    [Fact]
    public void TestGenerating20()
    {
        var leagueGeneration = new LeagueGeneration(20);
        leagueGeneration.Search();
        leagueGeneration.GenerateFixtures();

        Assert.Equal(20, leagueGeneration.FixtureWeeks[0][1]);
        Assert.Equal(21, leagueGeneration.FixtureWeeks[0][2]);
        Assert.Equal(22, leagueGeneration.FixtureWeeks[0][3]);

        Assert.Equal(30, leagueGeneration.FixtureWeeks[5][6]);
        Assert.Equal(31, leagueGeneration.FixtureWeeks[5][7]);
        Assert.Equal(32, leagueGeneration.FixtureWeeks[5][8]);

        Assert.Equal(17, leagueGeneration.FixtureWeeks[17][0]);
        Assert.Equal(18, leagueGeneration.FixtureWeeks[17][1]);
        Assert.Equal(19, leagueGeneration.FixtureWeeks[17][2]);

        Assert.Equal(0, leagueGeneration.State.Backtracks);
        Assert.Single(leagueGeneration.State.Solutions);
    }
}
