/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Decider.Performance.Calibration;

public static class CalibrationRunner
{
	public static void Run(IList<string> args)
	{
		if (args.Count > 0 && args[0] == "micro")
		{
			MicroTests.Run();
			return;
		}

		if (args.Count > 0 && args[0] == "bisect")
		{
			RunBisection(args);
			return;
		}

		if (args.Count > 0 && args[0] == "verify")
		{
			RunScheduleVerification(args);
			return;
		}

		if (args.Count == 0 || (args[0] != "validate" && args[0] != "probe" && args[0] != "decision"))
		{
			Console.WriteLine("Usage: calibrate <validate|probe|decision> [--cap <seconds>] [--family <rcpsp|jssp>] [--instance <name>] [--bound <value>]");
			return;
		}

		var mode = args[0];
		var cap = mode == "validate" ? 30 : 60;
		string family = null;
		string instanceName = null;
		var bound = -1;

		for (var i = 1; i < args.Count - 1; ++i)
		{
			if (args[i] == "--cap")
				cap = int.Parse(args[i + 1]);
			else if (args[i] == "--family")
				family = args[i + 1].ToLowerInvariant();
			else if (args[i] == "--instance")
				instanceName = args[i + 1];
			else if (args[i] == "--bound")
				bound = int.Parse(args[i + 1]);
		}

		var instances = ProbeCatalogue.Instances
			.Where(inst => family == null || inst.Family.ToString().ToLowerInvariant() == family)
			.Where(inst => instanceName == null || inst.Name == instanceName)
			.ToList();

		if (instances.Count == 0)
		{
			Console.WriteLine("No matching instances.");
			return;
		}

		var dataDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Data");

		Console.WriteLine($"Mode: {mode} | Instances: {instances.Count} | Per-run cap: {cap}s | " +
			$"Worst-case total: {instances.Count * cap / 60.0:F0} min");
		Console.WriteLine();

		if (mode == "validate")
		{
			RunValidation(instances, dataDirectory, cap);
			return;
		}

		if (mode == "decision")
		{
			var encoding = DisjunctionEncoding.Global;
			if (args.Contains("--pairwise"))
				encoding = DisjunctionEncoding.PairwiseOr;
			if (args.Contains("--bigm"))
				encoding = DisjunctionEncoding.BigM;

			foreach (var instance in instances)
			{
				var decisionBound = bound >= 0 ? bound : instance.KnownOptimum;
				var outcome = DeciderProbe.RunDecision(instance, dataDirectory, cap, decisionBound, encoding);
				Console.WriteLine($"{instance.Name}: objective <= {decisionBound} ({encoding}) -> {outcome}");
			}
			return;
		}

		RunProbes(instances, dataDirectory, cap);
	}

	private static void RunBisection(IList<string> args)
	{
		var instanceName = "ft06";
		var cap = 30;
		var encodingFilter = "all";
		var backjumping = !args.Contains("--nobackjump");
		for (var i = 1; i < args.Count - 1; ++i)
		{
			if (args[i] == "--instance")
				instanceName = args[i + 1];
			else if (args[i] == "--cap")
				cap = int.Parse(args[i + 1]);
			else if (args[i] == "--encoding")
				encodingFilter = args[i + 1].ToLowerInvariant();
		}

		var dataDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Data");
		var full = JsspParser.Parse(Path.Combine(dataDirectory, "Jssp", $"{instanceName}.txt"));

		Console.WriteLine($"Bisecting {instanceName} by job-prefix; per-run cap {cap}s; " +
			$"worst-case total {(full.JobCount - 1) * (cap * 3 + 10) / 60.0:F0} min");
		Console.WriteLine("| k jobs | CP-SAT opt | Global | PairwiseOr | BigM |");
		Console.WriteLine("|---|---|---|---|---|");

		foreach (var k in Enumerable.Range(2, full.JobCount - 1))
		{
			var prefix = new JsspInstance(k, full.MachineCount,
				full.Jobs.Take(k).ToList());

			var (status, optimum) = CpSatValidator.SolveJssp(prefix, 10);
			if (status != "OPTIMAL")
			{
				Console.WriteLine($"| {k} | {status} (skipping) | | | |");
				continue;
			}

			var global = encodingFilter == "all" || encodingFilter == "global"
				? DeciderProbe.RunJsspDecision(prefix, cap, (int) optimum, DisjunctionEncoding.Global, backjumping)
				: "-";
			var pairwise = encodingFilter == "all" || encodingFilter == "pairwise"
				? DeciderProbe.RunJsspDecision(prefix, cap, (int) optimum, DisjunctionEncoding.PairwiseOr, backjumping)
				: "-";
			var bigM = encodingFilter == "all" || encodingFilter == "bigm"
				? DeciderProbe.RunJsspDecision(prefix, cap, (int) optimum, DisjunctionEncoding.BigM, backjumping)
				: "-";
			var cumulative = encodingFilter == "cumulative"
				? DeciderProbe.RunJsspDecision(prefix, cap, (int) optimum, DisjunctionEncoding.CumulativeCapacityOne, backjumping)
				: "-";

			Console.WriteLine($"| {k} | {optimum} | {global} | {pairwise} | {bigM} | {cumulative} |");
			Console.Out.Flush();
		}
	}

	private static void RunScheduleVerification(IList<string> args)
	{
		var instanceName = "la19";
		var jobs = -1;
		var cap = 60;
		for (var i = 1; i < args.Count - 1; ++i)
		{
			if (args[i] == "--instance")
				instanceName = args[i + 1];
			else if (args[i] == "--jobs")
				jobs = int.Parse(args[i + 1]);
			else if (args[i] == "--cap")
				cap = int.Parse(args[i + 1]);
		}

		var dataDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Data");
		var full = JsspParser.Parse(Path.Combine(dataDirectory, "Jssp", $"{instanceName}.txt"));
		var instance = jobs > 0
			? new JsspInstance(jobs, full.MachineCount, full.Jobs.Take(jobs).ToList())
			: full;

		var (status, optimum, schedule) = CpSatValidator.SolveJsspWithSchedule(instance, 30);
		Console.WriteLine($"{instanceName} (jobs={instance.JobCount}): CP-SAT {status}, optimum {optimum}");
		if (status != "OPTIMAL")
			return;

		var outcome = DeciderProbe.VerifyFixedSchedule(instance, schedule, (int) optimum, cap);
		Console.WriteLine($"Decider with all starts fixed to CP-SAT's optimal schedule -> {outcome}");
		Console.WriteLine(outcome.StartsWith("Solved")
			? "Constraint network ACCEPTS the valid schedule: the bug is dynamic (pruning during search)."
			: "Constraint network REJECTS a valid schedule: the bug is static (constraint propagation/check).");
	}

	private static void RunValidation(IList<ProbeInstance> instances, string dataDirectory, int cap)
	{
		Console.WriteLine("| Instance | Family | Known | CP-SAT | Objective | Time (s) | Valid |");
		Console.WriteLine("|---|---|---|---|---|---|---|");

		var allValid = true;
		foreach (var instance in instances)
		{
			var result = CpSatValidator.Run(instance, dataDirectory, cap);
			allValid &= result.Valid;

			Console.WriteLine($"| {instance.Name} | {instance.Family} | {instance.KnownOptimum} | " +
				$"{result.Status} | {result.Objective} | {result.ElapsedSeconds:F1} | " +
				$"{(result.Valid ? "OK" : "MISMATCH")} |");
			Console.Out.Flush();
		}

		Console.WriteLine();
		Console.WriteLine(allValid
			? "All instances validated: models reproduce published optima."
			: "VALIDATION FAILURES PRESENT — check transcriptions before trusting probe results.");
	}

	private static void RunProbes(IList<ProbeInstance> instances, string dataDirectory, int cap)
	{
		Console.WriteLine("| Instance | Family | Evidence | Known | Status | Incumbent | Gap % | Backtracks | Time (s) | Sound |");
		Console.WriteLine("|---|---|---|---|---|---|---|---|---|---|");

		var allSound = true;
		foreach (var instance in instances)
		{
			var result = DeciderProbe.Run(instance, dataDirectory, cap);
			allSound &= result.Sound;

			var gap = result.GapPercent.HasValue ? result.GapPercent.Value.ToString("F1") : "-";
			var incumbent = result.Incumbent.HasValue ? result.Incumbent.Value.ToString() : "-";

			Console.WriteLine($"| {instance.Name} | {instance.Family} | {instance.DifficultyEvidence} | " +
				$"{instance.KnownOptimum} | {result.Status} | {incumbent} | {gap} | " +
				$"{result.Backtracks:N0} | {result.ElapsedSeconds:F1} | {(result.Sound ? "OK" : "UNSOUND")} |");
			Console.Out.Flush();
		}

		Console.WriteLine();
		Console.WriteLine(allSound
			? "No soundness violations (no incumbent below a published optimum; proofs match optima)."
			: "SOUNDNESS VIOLATIONS PRESENT — an incumbent beat a published optimum or a proof mismatched; investigate before anything else.");
	}
}
