/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;

using Decider.Csp.BaseTypes;
using Decider.Csp.Global;
using Decider.Csp.Integer;
using Decider.Example.Rcpsp;

namespace Decider.Performance.Calibration;

public class ProbeResult
{
	public ProbeInstance Instance { get; private set; }
	public string Status { get; private set; }
	public int? Incumbent { get; private set; }
	public int Backtracks { get; private set; }
	public double ElapsedSeconds { get; private set; }

	public ProbeResult(ProbeInstance instance, string status, int? incumbent, int backtracks, double elapsedSeconds)
	{
		this.Instance = instance;
		this.Status = status;
		this.Incumbent = incumbent;
		this.Backtracks = backtracks;
		this.ElapsedSeconds = elapsedSeconds;
	}

	public double? GapPercent
	{
		get
		{
			if (!this.Incumbent.HasValue)
				return null;

			return 100.0 * (this.Incumbent.Value - this.Instance.KnownOptimum) / this.Instance.KnownOptimum;
		}
	}

	public bool Sound
	{
		get
		{
			if (this.Status == "PROVEN")
				return this.Incumbent == this.Instance.KnownOptimum;

			if (this.Incumbent.HasValue)
				return this.Incumbent.Value >= this.Instance.KnownOptimum;

			return true;
		}
	}
}

public enum DisjunctionEncoding
{
	Global,
	PairwiseOr,
	BigM,
	CumulativeCapacityOne
}

public static class DeciderProbe
{
	public static ProbeResult Run(ProbeInstance instance, string dataDirectory, int capSeconds,
		bool clauseLearning = false)
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(capSeconds));
		var stopwatch = Stopwatch.StartNew();

		var (state, objectiveName) = instance.Family == ProbeFamily.Rcpsp
			? BuildRcpspModel(System.IO.Path.Combine(dataDirectory, instance.FileName))
			: BuildJsspModel(System.IO.Path.Combine(dataDirectory, "Jssp", instance.FileName));

		state.ClauseLearningEnabled = clauseLearning;

		var objective = state.Variables.First(v => v.Name == objectiveName);
		var result = state.Search(objective, cts.Token);
		stopwatch.Stop();

		var incumbent = state.OptimalSolution == null
			? (int?) null
			: state.OptimalSolution[objectiveName].InstantiatedValue;

		var status = result switch
		{
			StateOperationResult.Solved => "PROVEN",
			StateOperationResult.Cancelled => incumbent.HasValue ? "INCUMBENT" : "NOSOLUTION",
			_ => "UNSAT"
		};

		return new ProbeResult(instance, status, incumbent, state.Backtracks, stopwatch.Elapsed.TotalSeconds);
	}

	public static string RunDecision(ProbeInstance instance, string dataDirectory, int capSeconds, int bound,
		DisjunctionEncoding disjunctionEncoding = DisjunctionEncoding.Global)
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(capSeconds));

		var (state, objectiveName) = instance.Family == ProbeFamily.Rcpsp
			? BuildRcpspModel(System.IO.Path.Combine(dataDirectory, instance.FileName))
			: BuildJsspModel(System.IO.Path.Combine(dataDirectory, "Jssp", instance.FileName), disjunctionEncoding);

		var objective = (VariableInteger) state.Variables.First(v => v.Name == objectiveName);
		var constraints = state.Constraints.ToList();
		constraints.Add(new ConstraintInteger(objective <= bound));
		state.SetConstraints(constraints);

		var result = state.Search(cts.Token);
		if (result != StateOperationResult.Solved)
			return result.ToString();

		var value = state.Solutions.Last()[objectiveName].InstantiatedValue;
		return $"Solved with {objectiveName} = {value}";
	}

	public static string RunJsspDecision(JsspInstance instance, int capSeconds, int bound,
		DisjunctionEncoding disjunctionEncoding, bool backjumping = true)
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(capSeconds));
		var (state, objectiveName) = BuildJsspModel(instance, disjunctionEncoding);
		state.BackjumpingEnabled = backjumping;

		var objective = (VariableInteger) state.Variables.First(v => v.Name == objectiveName);
		var constraints = state.Constraints.ToList();
		constraints.Add(new ConstraintInteger(objective <= bound));
		state.SetConstraints(constraints);

		var result = state.Search(cts.Token);
		if (result != StateOperationResult.Solved)
			return result.ToString();

		return $"Solved({state.Solutions.Last()[objectiveName].InstantiatedValue})";
	}

	public static string VerifyFixedSchedule(JsspInstance instance, IList<IList<int>> schedule, int makespanBound,
		int capSeconds)
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(capSeconds));
		var (state, objectiveName) = BuildJsspModel(instance, DisjunctionEncoding.Global);

		var constraints = state.Constraints.ToList();
		foreach (var j in Enumerable.Range(0, instance.JobCount))
		{
			foreach (var k in Enumerable.Range(0, instance.Jobs[j].Count))
			{
				var variable = (VariableInteger) state.Variables.First(v => v.Name == $"s_{j}_{k}");
				constraints.Add(new ConstraintInteger(variable == schedule[j][k]));
			}
		}

		var objective = (VariableInteger) state.Variables.First(v => v.Name == objectiveName);
		constraints.Add(new ConstraintInteger(objective <= makespanBound));
		state.SetConstraints(constraints);

		var result = state.Search(cts.Token);
		if (result != StateOperationResult.Solved)
			return result.ToString();

		return $"Solved({state.Solutions.Last()[objectiveName].InstantiatedValue})";
	}

	private static (StateInteger State, string ObjectiveName) BuildRcpspModel(string instanceFile)
	{
		var instance = PspLibParser.Parse(instanceFile);
		var horizon = instance.Horizon;

		var starts = new List<IVariable<int>>();
		foreach (var i in Enumerable.Range(0, instance.JobCount))
			starts.Add(new VariableInteger(i.ToString(CultureInfo.InvariantCulture), 0, horizon));

		var constraints = new List<IConstraint>();

		foreach (var r in Enumerable.Range(0, instance.ResourceCount))
		{
			var demands = new List<int>();
			foreach (var jobDemands in instance.ResourceDemands)
				demands.Add(jobDemands[r]);

			constraints.Add(new CumulativeInteger(starts, instance.Durations, demands, instance.ResourceCapacities[r]));
		}

		constraints.Add(new ConstraintInteger((VariableInteger) starts[0] == 0));

		foreach (var j in Enumerable.Range(0, instance.JobCount))
		{
			foreach (var successor in instance.Successors[j])
			{
				constraints.Add(new ConstraintInteger(
					(VariableInteger) starts[successor] >= (VariableInteger) starts[j] + instance.Durations[j]));
			}
		}

		var state = new StateInteger(starts, constraints, new DomWdegOrdering(starts, constraints), new LowestValueOrdering());
		return (state, (instance.JobCount - 1).ToString(CultureInfo.InvariantCulture));
	}

	private static (StateInteger State, string ObjectiveName) BuildJsspModel(string instanceFile,
		DisjunctionEncoding disjunctionEncoding = DisjunctionEncoding.Global)
	{
		return BuildJsspModel(JsspParser.Parse(instanceFile), disjunctionEncoding);
	}

	public static (StateInteger State, string ObjectiveName) BuildJsspModel(JsspInstance instance,
		DisjunctionEncoding disjunctionEncoding = DisjunctionEncoding.Global)
	{
		var selectors = new List<VariableInteger>();
		var horizon = instance.Horizon;

		var starts = new List<IList<VariableInteger>>();
		foreach (var j in Enumerable.Range(0, instance.JobCount))
		{
			var jobStarts = new List<VariableInteger>();
			foreach (var k in Enumerable.Range(0, instance.Jobs[j].Count))
				jobStarts.Add(new VariableInteger($"s_{j}_{k}", 0, horizon - instance.Jobs[j][k].Duration));

			starts.Add(jobStarts);
		}

		var makespan = new VariableInteger("makespan", 0, horizon);
		var constraints = new List<IConstraint>();

		foreach (var j in Enumerable.Range(0, instance.JobCount))
		{
			foreach (var k in Enumerable.Range(0, instance.Jobs[j].Count - 1))
			{
				constraints.Add(new ConstraintInteger(
					starts[j][k + 1] >= starts[j][k] + instance.Jobs[j][k].Duration));
			}

			var lastIndex = instance.Jobs[j].Count - 1;
			constraints.Add(new ConstraintInteger(
				makespan >= starts[j][lastIndex] + instance.Jobs[j][lastIndex].Duration));
		}

		foreach (var m in Enumerable.Range(0, instance.MachineCount))
		{
			var machineStarts = new List<VariableInteger>();
			var machineDurations = new List<int>();

			foreach (var j in Enumerable.Range(0, instance.JobCount))
			{
				foreach (var k in Enumerable.Range(0, instance.Jobs[j].Count))
				{
					if (instance.Jobs[j][k].Machine != m)
						continue;

					machineStarts.Add(starts[j][k]);
					machineDurations.Add(instance.Jobs[j][k].Duration);
				}
			}

			if (disjunctionEncoding == DisjunctionEncoding.Global)
			{
				constraints.Add(new DisjunctiveInteger(machineStarts, machineDurations));
				continue;
			}

			if (disjunctionEncoding == DisjunctionEncoding.CumulativeCapacityOne)
			{
				var unitDemands = machineStarts.Select(_ => 1).ToList();
				constraints.Add(new CumulativeInteger(
					machineStarts.Cast<IVariable<int>>().ToList(), machineDurations, unitDemands, 1));
				continue;
			}

			foreach (var a in Enumerable.Range(0, machineStarts.Count))
			{
				foreach (var b in Enumerable.Range(a + 1, machineStarts.Count - a - 1))
				{
					if (disjunctionEncoding == DisjunctionEncoding.PairwiseOr)
					{
						constraints.Add(new ConstraintInteger(
							(machineStarts[a] >= machineStarts[b] + machineDurations[b]) |
							(machineStarts[b] >= machineStarts[a] + machineDurations[a])));
						continue;
					}

					var selector = new VariableInteger($"sel_{m}_{machineStarts[a].Name}_{machineStarts[b].Name}", 0, 1);
					selectors.Add(selector);
					constraints.Add(new ConstraintInteger(
						machineStarts[a] >= machineStarts[b] + machineDurations[b] - selector * horizon));
					constraints.Add(new ConstraintInteger(
						machineStarts[b] >= machineStarts[a] + machineDurations[a] - (1 - selector) * horizon));
				}
			}
		}

		var variables = starts.SelectMany(jobStarts => jobStarts).Cast<IVariable<int>>().ToList();
		variables.AddRange(selectors);
		variables.Add(makespan);

		var state = new StateInteger(variables, constraints, new DomWdegOrdering(variables, constraints), new LowestValueOrdering());
		return (state, "makespan");
	}
}
