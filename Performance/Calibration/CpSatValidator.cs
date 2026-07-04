/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

using Decider.Example.Rcpsp;
using Google.OrTools.Sat;

namespace Decider.Performance.Calibration;

public class ValidationResult
{
	public ProbeInstance Instance { get; private set; }
	public string Status { get; private set; }
	public long Objective { get; private set; }
	public double ElapsedSeconds { get; private set; }

	public ValidationResult(ProbeInstance instance, string status, long objective, double elapsedSeconds)
	{
		this.Instance = instance;
		this.Status = status;
		this.Objective = objective;
		this.ElapsedSeconds = elapsedSeconds;
	}

	public bool Valid => this.Status == "OPTIMAL" && this.Objective == this.Instance.KnownOptimum;
}

public static class CpSatValidator
{
	public static ValidationResult Run(ProbeInstance instance, string dataDirectory, int capSeconds)
	{
		var model = instance.Family == ProbeFamily.Rcpsp
			? BuildRcpspModel(Path.Combine(dataDirectory, instance.FileName))
			: BuildJsspModel(Path.Combine(dataDirectory, "Jssp", instance.FileName));

		var solver = new CpSolver
		{
			StringParameters = $"max_time_in_seconds:{capSeconds} num_workers:8"
		};

		var stopwatch = Stopwatch.StartNew();
		var status = solver.Solve(model);
		stopwatch.Stop();

		var objective = status == CpSolverStatus.Optimal || status == CpSolverStatus.Feasible
			? (long) solver.ObjectiveValue
			: -1;

		return new ValidationResult(instance, status.ToString().ToUpperInvariant(), objective, stopwatch.Elapsed.TotalSeconds);
	}

	private static CpModel BuildRcpspModel(string instanceFile)
	{
		var instance = PspLibParser.Parse(instanceFile);
		var horizon = instance.Horizon;
		var model = new CpModel();

		var starts = new List<IntVar>();
		var intervals = new List<IntervalVar>();
		foreach (var j in Enumerable.Range(0, instance.JobCount))
		{
			var start = model.NewIntVar(0, horizon, $"start_{j.ToString(CultureInfo.InvariantCulture)}");
			starts.Add(start);
			intervals.Add(model.NewFixedSizeIntervalVar(start, instance.Durations[j], $"interval_{j}"));
		}

		model.Add(starts[0] == 0);

		foreach (var j in Enumerable.Range(0, instance.JobCount))
		{
			foreach (var successor in instance.Successors[j])
				model.Add(starts[successor] >= starts[j] + instance.Durations[j]);
		}

		foreach (var r in Enumerable.Range(0, instance.ResourceCount))
		{
			var cumulative = model.AddCumulative(instance.ResourceCapacities[r]);
			foreach (var j in Enumerable.Range(0, instance.JobCount))
			{
				if (instance.ResourceDemands[j][r] == 0)
					continue;

				cumulative.AddDemand(intervals[j], instance.ResourceDemands[j][r]);
			}
		}

		model.Minimize(starts[instance.JobCount - 1]);
		return model;
	}

	public static (string Status, long Objective) SolveJssp(JsspInstance instance, int capSeconds)
	{
		var (status, objective, _) = SolveJsspWithSchedule(instance, capSeconds);
		return (status, objective);
	}

	public static (string Status, long Objective, IList<IList<int>> Starts) SolveJsspWithSchedule(
		JsspInstance instance, int capSeconds)
	{
		var model = BuildJsspModel(instance);
		var solver = new CpSolver
		{
			StringParameters = $"max_time_in_seconds:{capSeconds} num_workers:8"
		};

		var status = solver.Solve(model);
		if (status != CpSolverStatus.Optimal && status != CpSolverStatus.Feasible)
			return (status.ToString().ToUpperInvariant(), -1, new List<IList<int>>());

		var starts = new List<IList<int>>();
		foreach (var j in Enumerable.Range(0, instance.JobCount))
		{
			var jobStarts = new List<int>();
			foreach (var k in Enumerable.Range(0, instance.Jobs[j].Count))
				jobStarts.Add((int) solver.Value(LastStartVars[(j, k)]));

			starts.Add(jobStarts);
		}

		return (status.ToString().ToUpperInvariant(), (long) solver.ObjectiveValue, starts);
	}

	private static Dictionary<(int Job, int Op), IntVar> LastStartVars { get; set; } = new Dictionary<(int, int), IntVar>();

	private static CpModel BuildJsspModel(string instanceFile)
	{
		return BuildJsspModel(JsspParser.Parse(instanceFile));
	}

	private static CpModel BuildJsspModel(JsspInstance instance)
	{
		var horizon = instance.Horizon;
		var model = new CpModel();
		LastStartVars.Clear();

		var machineIntervals = new List<List<IntervalVar>>();
		foreach (var _ in Enumerable.Range(0, instance.MachineCount))
			machineIntervals.Add(new List<IntervalVar>());

		var lastEnds = new List<IntVar>();

		foreach (var j in Enumerable.Range(0, instance.JobCount))
		{
			IntVar previousEnd = null;
			foreach (var k in Enumerable.Range(0, instance.Jobs[j].Count))
			{
				var operation = instance.Jobs[j][k];
				var start = model.NewIntVar(0, horizon, $"start_{j}_{k}");
				LastStartVars[(j, k)] = start;
				var end = model.NewIntVar(0, horizon, $"end_{j}_{k}");
				var interval = model.NewIntervalVar(start, operation.Duration, end, $"interval_{j}_{k}");

				machineIntervals[operation.Machine].Add(interval);

				if (previousEnd != null)
					model.Add(start >= previousEnd);

				previousEnd = end;
			}

			lastEnds.Add(previousEnd);
		}

		foreach (var m in Enumerable.Range(0, instance.MachineCount))
			model.AddNoOverlap(machineIntervals[m]);

		var makespan = model.NewIntVar(0, horizon, "makespan");
		model.AddMaxEquality(makespan, lastEnds);
		model.Minimize(makespan);
		return model;
	}
}
