/*
  Copyright © Iain McDonald 2010-2026

  This file is part of Decider.
*/
using System;
using System.Collections.Generic;
using System.Globalization;

using Decider.Csp.BaseTypes;
using Decider.Csp.Global;
using Decider.Csp.Integer;

namespace Decider.Example.NQueens;

public class NQueens
{
	private int NumberOfQueens { get; set; }
	private IList<VariableInteger> Variables { get; set; }
	private IList<IConstraint> Constraints { get; set; }
	public IState<int> State { get; private set; }
	public IList<IDictionary<string, IVariable<int>>> Solutions { get; private set; }

	public NQueens(int numberOfQueens)
	{
		NumberOfQueens = numberOfQueens;

		// Model
		Variables = new VariableInteger[numberOfQueens];
		for (var i = 0; i < Variables.Count; ++i)
			Variables[i] = new VariableInteger(i.ToString(CultureInfo.CurrentCulture), 0, numberOfQueens - 1);

		//	Constraints
		Constraints = new List<IConstraint> { new AllDifferentInteger(Variables) };
		for (var i=0; i < Variables.Count - 1; ++i)
			for (var j = i + 1; j < Variables.Count; ++j)
			{
				Constraints.Add(new ConstraintInteger(Variables[i] - Variables[j] != j - i));
				Constraints.Add(new ConstraintInteger(Variables[i] - Variables[j] != i - j));
			}
	}

	public void SearchAllSolutions()
	{
		//	Search
		State = new StateInteger(Variables, Constraints);
		State.OnProgress = progress =>
		{
			var filled = (int)(progress * 50);
			Console.Write($"\r[{new string('#', filled)}{new string('-', 50 - filled)}] {progress:P1}  " +
				$"{State.Backtracks} backtracks, {State.Solutions.Count} solutions");
		};

		if (State.SearchAllSolutions() == StateOperationResult.Solved)
			Solutions = State.Solutions;
		else
		{
			Console.WriteLine("${State.Solutions.Count}");
			throw new ApplicationException("NQueens problem has no solutions.");
		}
	}
}
