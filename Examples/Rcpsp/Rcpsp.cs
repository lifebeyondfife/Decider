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
	private PspLibInstance Instance { get; set; }
	private IList<VariableInteger> StartTimes { get; set; }
	private IList<IConstraint> Constraints { get; set; }
	public IState<int> State { get; private set; }
	public IDictionary<string, IVariable<int>> Solution { get; private set; }

	public int TaskCount => this.Instance.JobCount;
	public int SinkTaskIndex => this.Instance.JobCount - 1;

	public Rcpsp(string instanceFile)
	{
		this.Instance = PspLibParser.Parse(instanceFile);

		this.StartTimes = new List<VariableInteger>();
		foreach (var i in Enumerable.Range(0, this.Instance.JobCount))
			this.StartTimes.Add(new VariableInteger(i.ToString(CultureInfo.CurrentCulture), 0, this.Instance.Horizon));

		this.Constraints = new List<IConstraint>();

		foreach (var r in Enumerable.Range(0, this.Instance.ResourceCount))
		{
			var demands = this.Instance.ResourceDemands.Select(d => d[r]).ToList();
			this.Constraints.Add(new CumulativeInteger(
				this.StartTimes.Cast<IVariable<int>>(),
				this.Instance.Durations, demands, this.Instance.ResourceCapacities[r]));
		}

		this.Constraints.Add(new ConstraintInteger(this.StartTimes[0] == 0));

		foreach (var j in Enumerable.Range(0, this.Instance.JobCount))
		{
			foreach (var successor in this.Instance.Successors[j])
			{
				this.Constraints.Add(new ConstraintInteger(
					this.StartTimes[successor] >= this.StartTimes[j] + this.Instance.Durations[j]));
			}
		}
	}

	public void OptimiseMakespan()
	{
		var variables = this.StartTimes.Cast<IVariable<int>>().ToList();
		this.State = new StateInteger(variables, this.Constraints,
			new DomWdegOrdering(variables, this.Constraints), new LowestValueOrdering());

		if (this.State.Search(this.StartTimes.Last()) == StateOperationResult.Solved)
			this.Solution = this.State.OptimalSolution;
		else
			throw new ApplicationException("RCPSP instance has no feasible solution.");
	}
}
