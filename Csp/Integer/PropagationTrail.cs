/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System;
using System.Collections.Generic;

using Decider.Csp.BaseTypes;

namespace Decider.Csp.Integer;

internal class PropagationTrail
{
	internal const int ReasonDecision = 0;
	internal const int ReasonConstraint = 1;
	internal const int ReasonLearnedClause = 2;

	internal struct Entry
	{
		internal int VariableId;
		internal bool IsLowerBound;
		internal int NewBound;
		internal int DecisionLevel;
		internal int ReasonKind;
		internal int ReasonIndex;

		internal Entry(int variableId, bool isLowerBound, int newBound, int decisionLevel, int reasonKind, int reasonIndex)
		{
			this.VariableId = variableId;
			this.IsLowerBound = isLowerBound;
			this.NewBound = newBound;
			this.DecisionLevel = decisionLevel;
			this.ReasonKind = reasonKind;
			this.ReasonIndex = reasonIndex;
		}
	}

	private Entry[] entries;
	private IList<BoundReason>[] explanations;
	private int entryCount;
	private int[] levelStarts;
	private List<(int Bound, int Level)>[] lowerHistory;
	private List<(int Bound, int Level)>[] upperHistory;

	internal int Count => this.entryCount;

	internal PropagationTrail(int maxLevels, int estimatedCapacity)
	{
		this.entries = new Entry[estimatedCapacity];
		this.explanations = new IList<BoundReason>[estimatedCapacity];
		this.entryCount = 0;
		this.levelStarts = new int[maxLevels + 1];
		for (var i = 0; i < this.levelStarts.Length; ++i)
			this.levelStarts[i] = -1;

		this.lowerHistory = new List<(int, int)>[maxLevels];
		this.upperHistory = new List<(int, int)>[maxLevels];
	}

	internal void RecordDecision(int variableId, int lowerBound, int upperBound, int decisionLevel)
	{
		EnsureLevelStart(decisionLevel);
		AppendEntry(new Entry(variableId, true, lowerBound, decisionLevel, ReasonDecision, -1));
		AppendEntry(new Entry(variableId, false, upperBound, decisionLevel, ReasonDecision, -1));
	}

	internal void RecordPropagation(int variableId, bool isLowerBound, int newBound,
		int decisionLevel, int reasonKind, int reasonIndex, IList<BoundReason> explanation = null!)
	{
		EnsureLevelStart(decisionLevel);
		AppendEntry(new Entry(variableId, isLowerBound, newBound, decisionLevel, reasonKind, reasonIndex),
			explanation);
	}

	internal IList<BoundReason> GetExplanation(int index)
	{
		if (index < 0 || index >= this.entryCount)
			return null!;

		return this.explanations[index];
	}

	internal void Backtrack(int toLevel)
	{
		var markerIndex = toLevel + 1;
		if (markerIndex < 0 || markerIndex >= this.levelStarts.Length)
			return;

		var restoreFrom = this.levelStarts[markerIndex];
		if (restoreFrom == -1)
		{
			for (var i = this.entryCount - 1; i >= 0; --i)
			{
				if (this.entries[i].DecisionLevel <= toLevel)
				{
					TruncateHistory(i + 1);
					break;
				}
			}

			return;
		}

		TruncateHistory(restoreFrom);

		for (var i = markerIndex; i < this.levelStarts.Length; ++i)
			this.levelStarts[i] = -1;
	}

	internal int FindDecisionLevel(int variableId, bool isLowerBound, int boundValue)
	{
		var history = isLowerBound ? this.lowerHistory : this.upperHistory;
		if (variableId < 0 || variableId >= history.Length || history[variableId] == null)
			return 0;

		var changes = history[variableId];
		var low = 0;
		var high = changes.Count - 1;
		var found = -1;

		while (low <= high)
		{
			var mid = (low + high) / 2;
			var satisfies = isLowerBound
				? changes[mid].Bound >= boundValue
				: changes[mid].Bound <= boundValue;

			if (satisfies)
			{
				found = mid;
				high = mid - 1;
			}
			else
			{
				low = mid + 1;
			}
		}

		return found == -1 ? 0 : changes[found].Level;
	}

	private void TruncateHistory(int newCount)
	{
		for (var i = this.entryCount - 1; i >= newCount; --i)
		{
			ref var entry = ref this.entries[i];
			var history = entry.IsLowerBound ? this.lowerHistory : this.upperHistory;
			if (entry.VariableId >= 0 && entry.VariableId < history.Length && history[entry.VariableId] != null)
			{
				var changes = history[entry.VariableId];
				changes.RemoveAt(changes.Count - 1);
			}
		}

		this.entryCount = newCount;
	}

	internal ref Entry GetEntry(int index)
	{
		return ref this.entries[index];
	}

	internal int GetLevelStart(int level)
	{
		if (level < 0 || level >= this.levelStarts.Length)
			return this.entryCount;

		return this.levelStarts[level] == -1 ? this.entryCount : this.levelStarts[level];
	}

	internal void Clear()
	{
		this.entryCount = 0;
		for (var i = 0; i < this.levelStarts.Length; ++i)
			this.levelStarts[i] = -1;

		for (var i = 0; i < this.lowerHistory.Length; ++i)
		{
			this.lowerHistory[i]?.Clear();
			this.upperHistory[i]?.Clear();
		}
	}

	private void EnsureLevelStart(int level)
	{
		if (level < 0 || level >= this.levelStarts.Length)
			return;

		if (this.levelStarts[level] == -1)
			this.levelStarts[level] = this.entryCount;
	}

	private void AppendEntry(Entry entry, IList<BoundReason> explanation = null!)
	{
		if (this.entryCount >= this.entries.Length)
		{
			Array.Resize(ref this.entries, this.entries.Length * 2);
			Array.Resize(ref this.explanations, this.explanations.Length * 2);
		}

		RecordHistory(entry);
		this.explanations[this.entryCount] = explanation;
		this.entries[this.entryCount++] = entry;
	}

	private void RecordHistory(Entry entry)
	{
		if (entry.VariableId < 0)
			return;

		if (entry.VariableId >= this.lowerHistory.Length)
		{
			var newSize = entry.VariableId + 1;
			Array.Resize(ref this.lowerHistory, newSize);
			Array.Resize(ref this.upperHistory, newSize);
		}

		var history = entry.IsLowerBound ? this.lowerHistory : this.upperHistory;
		history[entry.VariableId] ??= new List<(int, int)>();
		history[entry.VariableId].Add((entry.NewBound, entry.DecisionLevel));
	}
}
