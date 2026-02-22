/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System.Collections.Generic;

using Decider.Csp.BaseTypes;

namespace Decider.Csp.Integer;

internal class ClauseStore
{
	private const double ActivityDecayFactor = 0.999;
	private const int DefaultMaxClauses = 4000;

	private struct WatchEntry
	{
		internal int ClauseIndex;
		internal int LiteralIndex;

		internal WatchEntry(int clauseIndex, int literalIndex)
		{
			this.ClauseIndex = clauseIndex;
			this.LiteralIndex = literalIndex;
		}
	}

	private List<Clause> Clauses { get; set; }
	private Dictionary<(int VariableId, bool WatchesLowerBound), List<WatchEntry>> WatchLists { get; set; }
	private double ActivityIncrement { get; set; }
	internal int MaxClauses { get; set; }

	internal int Count => this.Clauses.Count;

	internal ClauseStore()
	{
		this.Clauses = new List<Clause>();
		this.WatchLists = new Dictionary<(int, bool), List<WatchEntry>>();
		this.ActivityIncrement = 1.0;
		this.MaxClauses = DefaultMaxClauses;
	}

	internal int AddClause(BoundReason[] literals, IList<IVariable<int>> variables)
	{
		var clause = new Clause(literals);
		var clauseIndex = this.Clauses.Count;
		this.Clauses.Add(clause);

		if (literals.Length < 2)
			return clauseIndex;

		var bestIndices = FindTwoBestWatches(literals, variables);
		clause.Watch1 = bestIndices[0];
		clause.Watch2 = bestIndices[1];

		AddWatch(literals[clause.Watch1], clauseIndex, clause.Watch1);
		AddWatch(literals[clause.Watch2], clauseIndex, clause.Watch2);

		return clauseIndex;
	}

	internal bool NotifyBoundChange(int variableId, bool isLowerBound, IList<IVariable<int>> variables,
		out BoundReason forcedLiteral, out int forcedClauseIndex)
	{
		forcedLiteral = default;
		forcedClauseIndex = -1;

		var watchKey = (variableId, isLowerBound);
		if (!this.WatchLists.TryGetValue(watchKey, out var watchList))
			return false;

		for (var wi = watchList.Count - 1; wi >= 0; --wi)
		{
			var watch = watchList[wi];
			if (watch.ClauseIndex >= this.Clauses.Count)
			{
				watchList.RemoveAt(wi);
				continue;
			}

			var clause = this.Clauses[watch.ClauseIndex];
			var watchedLiteral = clause.Literals[watch.LiteralIndex];
			var variable = variables[watchedLiteral.VariableIndex];

			if (!Clause.IsLiteralFalsified(watchedLiteral, variable))
				continue;

			var otherWatchIdx = watch.LiteralIndex == clause.Watch1 ? clause.Watch2 : clause.Watch1;
			var otherLiteral = clause.Literals[otherWatchIdx];
			var otherVariable = variables[otherLiteral.VariableIndex];

			if (Clause.IsLiteralSatisfied(otherLiteral, otherVariable))
				continue;

			var replacementFound = false;
			for (var li = 0; li < clause.Literals.Length; ++li)
			{
				if (li == clause.Watch1 || li == clause.Watch2)
					continue;

				var candidateLiteral = clause.Literals[li];
				var candidateVariable = variables[candidateLiteral.VariableIndex];

				if (Clause.IsLiteralFalsified(candidateLiteral, candidateVariable))
					continue;

				if (watch.LiteralIndex == clause.Watch1)
					clause.Watch1 = li;
				else
					clause.Watch2 = li;

				watchList.RemoveAt(wi);
				AddWatch(candidateLiteral, watch.ClauseIndex, li);
				replacementFound = true;
				break;
			}

			if (replacementFound)
				continue;

			if (Clause.IsLiteralFalsified(otherLiteral, otherVariable))
				return true;

			forcedLiteral = otherLiteral;
			forcedClauseIndex = watch.ClauseIndex;
		}

		return false;
	}

	internal void BumpActivity(int clauseIndex)
	{
		if (clauseIndex < 0 || clauseIndex >= this.Clauses.Count)
			return;

		this.Clauses[clauseIndex].Activity += this.ActivityIncrement;
	}

	internal void DecayAllActivities()
	{
		this.ActivityIncrement /= ActivityDecayFactor;
	}

	internal void ReduceDatabase()
	{
		if (this.Clauses.Count <= this.MaxClauses)
			return;

		var threshold = this.Clauses.Count / 2;
		var activities = new List<double>(this.Clauses.Count);
		foreach (var clause in this.Clauses)
			activities.Add(clause.Activity);

		activities.Sort();
		var cutoff = activities[threshold];

		var newClauses = new List<Clause>();
		var indexMap = new int[this.Clauses.Count];

		for (var i = 0; i < this.Clauses.Count; ++i)
		{
			if (this.Clauses[i].Literals.Length <= 2 || this.Clauses[i].Activity > cutoff)
			{
				indexMap[i] = newClauses.Count;
				newClauses.Add(this.Clauses[i]);
			}
			else
			{
				indexMap[i] = -1;
			}
		}

		this.Clauses = newClauses;
		RebuildWatchLists();
	}

	internal void Clear()
	{
		this.Clauses.Clear();
		this.WatchLists.Clear();
		this.ActivityIncrement = 1.0;
	}

	private void AddWatch(BoundReason literal, int clauseIndex, int literalIndex)
	{
		var key = (literal.VariableIndex, literal.IsLowerBound);
		if (!this.WatchLists.TryGetValue(key, out var list))
		{
			list = new List<WatchEntry>();
			this.WatchLists[key] = list;
		}

		list.Add(new WatchEntry(clauseIndex, literalIndex));
	}

	private void RebuildWatchLists()
	{
		this.WatchLists.Clear();

		for (var ci = 0; ci < this.Clauses.Count; ++ci)
		{
			var clause = this.Clauses[ci];
			if (clause.Literals.Length < 2)
				continue;

			AddWatch(clause.Literals[clause.Watch1], ci, clause.Watch1);
			AddWatch(clause.Literals[clause.Watch2], ci, clause.Watch2);
		}
	}

	private static int[] FindTwoBestWatches(BoundReason[] literals, IList<IVariable<int>> variables)
	{
		var best1 = 0;
		var best2 = 1;

		for (var i = 0; i < literals.Length; ++i)
		{
			var variable = variables[literals[i].VariableIndex];
			if (!Clause.IsLiteralFalsified(literals[i], variable))
			{
				if (Clause.IsLiteralFalsified(literals[best1], variables[literals[best1].VariableIndex]))
				{
					best1 = i;
				}
				else if (i != best1 && Clause.IsLiteralFalsified(literals[best2], variables[literals[best2].VariableIndex]))
				{
					best2 = i;
				}
			}
		}

		return new[] { best1, best2 };
	}
}
