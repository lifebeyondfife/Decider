/*
  Copyright © Iain McDonald 2010-2026

  This file is part of Decider.
*/
using System.Linq;
using Xunit;

using Decider.Example.NQueens;

namespace Decider.Tests.Example;

public class NQueensTest
{
    [Fact]
    public void TestCorrectSolution()
    {
        var nQueens = new NQueens(8);
        nQueens.SearchAllSolutions(false);
        var solution = nQueens.Solutions.First();

        Assert.Equal(0, solution["0"].InstantiatedValue);
        Assert.Equal(6, solution["1"].InstantiatedValue);
        Assert.Equal(3, solution["2"].InstantiatedValue);
        Assert.Equal(5, solution["3"].InstantiatedValue);
        Assert.Equal(7, solution["4"].InstantiatedValue);
        Assert.Equal(1, solution["5"].InstantiatedValue);
        Assert.Equal(4, solution["6"].InstantiatedValue);
        Assert.Equal(2, solution["7"].InstantiatedValue);
    }

    [Theory]
    [InlineData(4, 2, 14)]
    [InlineData(8, 92, 889)]
    [InlineData(10, 724, 10770)]
    public void TestNumberOfSolutions(int boardSize, int expectedSolutions, int expectedBacktracks)
    {
        var nQueens = new NQueens(boardSize);
        nQueens.SearchAllSolutions(false);

        Assert.Equal(expectedSolutions, nQueens.Solutions.Count);
        Assert.Equal(expectedBacktracks, nQueens.State.Backtracks);
    }
}
