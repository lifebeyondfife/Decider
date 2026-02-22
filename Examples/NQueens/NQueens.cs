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
		this.NumberOfQueens = numberOfQueens;

		// Model
		this.Variables = new VariableInteger[numberOfQueens];
		for (var i = 0; i < this.Variables.Count; ++i)
			Variables[i] = new VariableInteger(i.ToString(CultureInfo.CurrentCulture), 0, numberOfQueens - 1);

		//	Constraints
		this.Constraints = new List<IConstraint> { new AllDifferentInteger(this.Variables) };
		for (var i=0; i < this.Variables.Count - 1; ++i)
			for (var j = i + 1; j < this.Variables.Count; ++j)
			{
				this.Constraints.Add(new ConstraintInteger(this.Variables[i] - this.Variables[j] != j - i));
				this.Constraints.Add(new ConstraintInteger(this.Variables[i] - this.Variables[j] != i - j));
			}
	}

	public void SearchAllSolutions(bool progress = true)
	{
		//	Search
		this.State = new StateInteger(this.Variables, this.Constraints, new MostConstrainedOrdering(), new MiddleValueOrdering());
		((StateInteger)this.State).ClauseLearningEnabled = false;
		if (progress)
			this.State.OnProgress = progress =>
			{
				var filled = (int)(progress * 50);
				Console.Write($"\r[{new string('#', filled)}{new string('-', 50 - filled)}] {progress:P1}  " +
					$"{this.State.Backtracks} backtracks, {this.State.Solutions.Count} solutions");
			};

		if (this.State.SearchAllSolutions() == StateOperationResult.Solved)
			this.Solutions = this.State.Solutions;
		else
		{
			Console.WriteLine("${State.Solutions.Count}");
			throw new ApplicationException("NQueens problem has no solutions.");
		}
	}
}
