/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Decider.Performance.Calibration;

public class JsspOperation
{
	public int Machine { get; private set; }
	public int Duration { get; private set; }

	public JsspOperation(int machine, int duration)
	{
		this.Machine = machine;
		this.Duration = duration;
	}
}

public class JsspInstance
{
	public int JobCount { get; private set; }
	public int MachineCount { get; private set; }
	public IList<IList<JsspOperation>> Jobs { get; private set; }

	public JsspInstance(int jobCount, int machineCount, IList<IList<JsspOperation>> jobs)
	{
		this.JobCount = jobCount;
		this.MachineCount = machineCount;
		this.Jobs = jobs;
	}

	public int Horizon => this.Jobs.Sum(job => job.Sum(op => op.Duration));
}

public static class JsspParser
{
	public static JsspInstance Parse(string filePath)
	{
		var dataLines = File.ReadAllLines(filePath)
			.Select(line => line.Trim())
			.Where(line => line.Length > 0 && !line.StartsWith("#"))
			.ToList();

		if (dataLines.Count == 0)
			throw new InvalidDataException($"No data lines found in '{filePath}'.");

		var header = SplitInts(dataLines[0]);
		var jobCount = header[0];
		var machineCount = header[1];

		if (dataLines.Count < jobCount + 1)
			throw new InvalidDataException($"Expected {jobCount} job lines in '{filePath}', found {dataLines.Count - 1}.");

		var jobs = new List<IList<JsspOperation>>();
		foreach (var jobIndex in Enumerable.Range(0, jobCount))
		{
			var values = SplitInts(dataLines[jobIndex + 1]);
			if (values.Count != machineCount * 2)
				throw new InvalidDataException(
					$"Job line {jobIndex} in '{filePath}' has {values.Count} values, expected {machineCount * 2}.");

			var operations = new List<JsspOperation>();
			foreach (var opIndex in Enumerable.Range(0, machineCount))
				operations.Add(new JsspOperation(values[opIndex * 2], values[opIndex * 2 + 1]));

			jobs.Add(operations);
		}

		return new JsspInstance(jobCount, machineCount, jobs);
	}

	private static IList<int> SplitInts(string line)
	{
		return line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
			.Select(int.Parse)
			.ToList();
	}
}
