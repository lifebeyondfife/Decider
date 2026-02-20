/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Decider.Csp.BaseTypes;
using Decider.Csp.Global;
using Decider.Csp.Integer;

namespace Decider.Example.Rcpsp;

public class Rcpsp
{
	private int NumberOfTasks { get; set; }
	private IList<VariableInteger> StartTimes { get; set; }
	private IList<IConstraint> Constraints { get; set; }
	public IState<int> State { get; private set; }
	public IDictionary<string, IVariable<int>> Solution { get; private set; }

	public Rcpsp()
	{
		this.NumberOfTasks = 10;
		var horizon = 30;

		this.StartTimes = [];
		foreach (var i in Enumerable.Range(0, NumberOfTasks))
			StartTimes.Add(new VariableInteger(i.ToString(CultureInfo.CurrentCulture), 0, horizon));

		var durations = new List<int> { 0, 3, 2, 5, 4, 2, 3, 4, 2, 0 };
		var demands = new List<int> { 0, 2, 1, 2, 1, 2, 1, 2, 1, 0 };
		var capacity = 3;

		this.Constraints =
        [
            new CumulativeInteger(this.StartTimes.Cast<IVariable<int>>(), durations, demands, capacity),
            new ConstraintInteger(this.StartTimes[0] == 0),
            new ConstraintInteger(this.StartTimes[1] >= this.StartTimes[0] + durations[0]),
            new ConstraintInteger(this.StartTimes[2] >= this.StartTimes[0] + durations[0]),
            new ConstraintInteger(this.StartTimes[3] >= this.StartTimes[1] + durations[1]),
            new ConstraintInteger(this.StartTimes[4] >= this.StartTimes[1] + durations[1]),
            new ConstraintInteger(this.StartTimes[5] >= this.StartTimes[2] + durations[2]),
            new ConstraintInteger(this.StartTimes[6] >= this.StartTimes[3] + durations[3]),
            new ConstraintInteger(this.StartTimes[7] >= this.StartTimes[4] + durations[4]),
            new ConstraintInteger(this.StartTimes[8] >= this.StartTimes[5] + durations[5]),
            new ConstraintInteger(this.StartTimes[9] >= this.StartTimes[6] + durations[6]),
            new ConstraintInteger(this.StartTimes[9] >= this.StartTimes[7] + durations[7]),
            new ConstraintInteger(this.StartTimes[9] >= this.StartTimes[8] + durations[8])
        ];
	}

	public void Solve()
	{
		this.State = new StateInteger(StartTimes.Cast<IVariable<int>>(), Constraints,
			new DomWdegOrdering(this.StartTimes.Cast<IVariable<int>>(), this.Constraints));

		if (this.State.Search() == StateOperationResult.Solved)
			this.Solution = State.Solutions[0];
		else
			throw new ApplicationException("RCPSP problem has no solution.");
	}

	public void OptimiseMakespan()
	{
		this.State = new StateInteger(this.StartTimes, this.Constraints);

		if (this.State.Search(StartTimes.Last()) == StateOperationResult.Solved)
			this.Solution = this.State.OptimalSolution;
		else
			throw new ApplicationException("RCPSP problem has no solution.");
	}
}
