/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System;
using System.Collections.Generic;
using System.Linq;

using Decider.Csp.BaseTypes;
using Decider.Csp.Integer;

namespace Decider.Performance.Calibration;

public static class MicroTests
{
	public static void Run()
	{
		TestSimpleInequality();
		TestPairwiseOr();
		TestBigM();
	}

	private static void TestSimpleInequality()
	{
		var s1 = new VariableInteger("s1", 0, 10);
		var s2 = new VariableInteger("s2", 0, 10);
		var constraints = new List<IConstraint> { new ConstraintInteger(s2 >= s1 + 5) };

		var expected = CountBruteForce(11, 11, (a, b) => b >= a + 5);
		var actual = SolveAll(new List<IVariable<int>> { s1, s2 }, constraints);

		Report("simple s2 >= s1 + 5", expected, actual);
	}

	private static void TestPairwiseOr()
	{
		var s1 = new VariableInteger("s1", 0, 10);
		var s2 = new VariableInteger("s2", 0, 10);
		var constraints = new List<IConstraint>
		{
			new ConstraintInteger((s1 >= s2 + 5) | (s2 >= s1 + 5))
		};

		var expected = CountBruteForce(11, 11, (a, b) => a >= b + 5 || b >= a + 5);
		var actual = SolveAll(new List<IVariable<int>> { s1, s2 }, constraints);

		Report("or-disjunction (s1 >= s2+5) | (s2 >= s1+5)", expected, actual);
	}

	private static void TestBigM()
	{
		const int bigM = 20;
		var s1 = new VariableInteger("s1", 0, 10);
		var s2 = new VariableInteger("s2", 0, 10);
		var sel = new VariableInteger("sel", 0, 1);
		var constraints = new List<IConstraint>
		{
			new ConstraintInteger(s1 >= s2 + 5 - sel * bigM),
			new ConstraintInteger(s2 >= s1 + 5 - (1 - sel) * bigM)
		};

		var expected = 0;
		foreach (var a in Enumerable.Range(0, 11))
		{
			foreach (var b in Enumerable.Range(0, 11))
			{
				foreach (var y in Enumerable.Range(0, 2))
				{
					if (a >= b + 5 - y * bigM && b >= a + 5 - (1 - y) * bigM)
						++expected;
				}
			}
		}

		var actual = SolveAll(new List<IVariable<int>> { s1, s2, sel }, constraints);

		Report("big-M pair with 0/1 selector", expected, actual);
	}

	private static int CountBruteForce(int range1, int range2, Func<int, int, bool> predicate)
	{
		var count = 0;
		foreach (var a in Enumerable.Range(0, range1))
		{
			foreach (var b in Enumerable.Range(0, range2))
			{
				if (predicate(a, b))
					++count;
			}
		}
		return count;
	}

	private static int SolveAll(IList<IVariable<int>> variables, IList<IConstraint> constraints)
	{
		var state = new StateInteger(variables, constraints);
		state.SearchAllSolutions();
		return state.Solutions.Count;
	}

	private static void Report(string name, int expected, int actual)
	{
		var verdict = expected == actual ? "OK" : "MISMATCH";
		Console.WriteLine($"{verdict}: {name} — brute force {expected}, Decider {actual}");
	}
}
