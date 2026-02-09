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
		NumberOfTasks = 10;
		var horizon = 30;

		StartTimes = [];
		foreach (var i in Enumerable.Range(0, NumberOfTasks))
			StartTimes.Add(new VariableInteger(i.ToString(CultureInfo.CurrentCulture), 0, horizon));

		var durations = new List<int> { 0, 3, 2, 5, 4, 2, 3, 4, 2, 0 };
		var demands = new List<int> { 0, 2, 1, 2, 1, 2, 1, 2, 1, 0 };
		var capacity = 3;

		Constraints = new List<IConstraint>
        {
            new CumulativeInteger(StartTimes, durations, demands, capacity),
            new ConstraintInteger(StartTimes[0] == 0),
            new ConstraintInteger(StartTimes[1] >= StartTimes[0] + durations[0]),
            new ConstraintInteger(StartTimes[2] >= StartTimes[0] + durations[0]),
            new ConstraintInteger(StartTimes[3] >= StartTimes[1] + durations[1]),
            new ConstraintInteger(StartTimes[4] >= StartTimes[1] + durations[1]),
            new ConstraintInteger(StartTimes[5] >= StartTimes[2] + durations[2]),
            new ConstraintInteger(StartTimes[6] >= StartTimes[3] + durations[3]),
            new ConstraintInteger(StartTimes[7] >= StartTimes[4] + durations[4]),
            new ConstraintInteger(StartTimes[8] >= StartTimes[5] + durations[5]),
            new ConstraintInteger(StartTimes[9] >= StartTimes[6] + durations[6]),
            new ConstraintInteger(StartTimes[9] >= StartTimes[7] + durations[7]),
            new ConstraintInteger(StartTimes[9] >= StartTimes[8] + durations[8])
        };
	}

	public void Solve()
	{
		State = new StateInteger(StartTimes, Constraints);

		if (State.Search() == StateOperationResult.Solved)
			Solution = State.Solutions[0];
		else
			throw new ApplicationException("RCPSP problem has no solution.");
	}

	public void OptimiseMakespan()
	{
		State = new StateInteger(StartTimes, Constraints);

		if (State.Search(StartTimes.Last()) == StateOperationResult.Solved)
			Solution = State.OptimalSolution;
		else
			throw new ApplicationException("RCPSP problem has no solution.");
	}
}
