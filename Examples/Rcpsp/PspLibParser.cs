/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Decider.Example.Rcpsp;

public class PspLibInstance
{
	public int JobCount { get; set; }
	public int Horizon { get; set; }
	public int ResourceCount { get; set; }
	public IList<int> Durations { get; set; }
	public IList<IList<int>> ResourceDemands { get; set; }
	public IList<int> ResourceCapacities { get; set; }
	public IList<IList<int>> Successors { get; set; }
}

public static class PspLibParser
{
	public static PspLibInstance Parse(string filePath)
	{
		var lines = File.ReadAllLines(filePath);
		var instance = new PspLibInstance();

		foreach (var li in lines.Select((l, i) => new { Line = l.Trim(), Index = i } ))
		{
			if (li.Line.StartsWith("jobs"))
			{
				var parts = li.Line.Split(':');
				instance.JobCount = int.Parse(parts[1].Trim());
			}
			else if (li.Line.StartsWith("horizon"))
			{
				var parts = li.Line.Split(':');
				instance.Horizon = int.Parse(parts[1].Trim());
			}
			else if (li.Line.Contains("- renewable"))
			{
				var parts = li.Line.Split(':');
				var resourcePart = parts[1].Trim().Split(' ')[0];
				instance.ResourceCount = int.Parse(resourcePart);
			}
			else if (li.Line == "PRECEDENCE RELATIONS:")
			{
				ParsePrecedenceRelations(lines, li.Index + 2, instance);
			}
			else if (li.Line == "REQUESTS/DURATIONS:")
			{
				ParseRequestsDurations(lines, li.Index + 3, instance);
			}
			else if (li.Line == "RESOURCEAVAILABILITIES:")
			{
				ParseResourceAvailabilities(lines, li.Index + 2, instance);
			}
		}

		return instance;
	}

	private static void ParsePrecedenceRelations(string[] lines, int startIndex, PspLibInstance instance)
	{
		instance.Successors = new List<IList<int>>();
		foreach (var _ in Enumerable.Range(0, instance.JobCount))
			instance.Successors.Add(new List<int>());

		var index = startIndex;
		while (index < lines.Length && !lines[index].StartsWith("***"))
		{
			var line = lines[index].Trim();
			if (string.IsNullOrWhiteSpace(line))
			{
				index++;
				continue;
			}

			var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < 3)
			{
				index++;
				continue;
			}

			var jobNr = int.Parse(parts[0]) - 1;
			var numSuccessors = int.Parse(parts[2]);

			for (var s = 0; s < numSuccessors && s + 3 < parts.Length; s++)
			{
				var successor = int.Parse(parts[3 + s]) - 1;
				instance.Successors[jobNr].Add(successor);
			}

			index++;
		}
	}

	private static void ParseRequestsDurations(string[] lines, int startIndex, PspLibInstance instance)
	{
		instance.Durations = [.. new int[instance.JobCount]];
		instance.ResourceDemands = new List<IList<int>>();
		foreach (var _ in Enumerable.Range(0, instance.JobCount))
			instance.ResourceDemands.Add(null);

		var index = startIndex;
		while (index < lines.Length && !lines[index].StartsWith("***"))
		{
			var line = lines[index].Trim();
			if (string.IsNullOrWhiteSpace(line))
			{
				index++;
				continue;
			}

			var parts = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < 3 + instance.ResourceCount)
			{
				index++;
				continue;
			}

			var jobNr = int.Parse(parts[0]) - 1;
			instance.Durations[jobNr] = int.Parse(parts[2]);
			instance.ResourceDemands[jobNr] = [.. new int[instance.ResourceCount]];

			for (var r = 0; r < instance.ResourceCount; r++)
			{
				instance.ResourceDemands[jobNr][r] = int.Parse(parts[3 + r]);
			}

			index++;
		}
	}

	private static void ParseResourceAvailabilities(string[] lines, int startIndex, PspLibInstance instance)
	{
		var index = startIndex;
		if (index >= lines.Length)
			return;

		var line = lines[index].Trim();
		var parts = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);

		instance.ResourceCapacities = new List<int>(new int[instance.ResourceCount]);
		for (var r = 0; r < instance.ResourceCount && r < parts.Length; r++)
		{
			instance.ResourceCapacities[r] = int.Parse(parts[r]);
		}
	}
}
