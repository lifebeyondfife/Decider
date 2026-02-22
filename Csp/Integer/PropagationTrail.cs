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
	private bool[] explanationApproximate;
	private int entryCount;
	private int[] levelStarts;

	internal int Count => this.entryCount;

	internal PropagationTrail(int maxLevels, int estimatedCapacity)
	{
		this.entries = new Entry[estimatedCapacity];
		this.explanations = new IList<BoundReason>[estimatedCapacity];
		this.explanationApproximate = new bool[estimatedCapacity];
		this.entryCount = 0;
		this.levelStarts = new int[maxLevels + 1];
		for (var i = 0; i < this.levelStarts.Length; ++i)
			this.levelStarts[i] = -1;
	}

	internal void RecordDecision(int variableId, int lowerBound, int upperBound, int decisionLevel)
	{
		EnsureLevelStart(decisionLevel);
		AppendEntry(new Entry(variableId, true, lowerBound, decisionLevel, ReasonDecision, -1));

		IList<BoundReason> ubExplanation = new List<BoundReason>
			{ new BoundReason(variableId, true, lowerBound) };
		AppendEntry(
			new Entry(variableId, false, upperBound, decisionLevel, ReasonConstraint, -1),
			ubExplanation);
	}

	internal void RecordPropagation(int variableId, bool isLowerBound, int newBound,
		int decisionLevel, int reasonKind, int reasonIndex, IList<BoundReason> explanation = null!,
		bool isApproximate = false)
	{
		EnsureLevelStart(decisionLevel);
		AppendEntry(new Entry(variableId, isLowerBound, newBound, decisionLevel, reasonKind, reasonIndex),
			explanation, isApproximate);
	}

	internal bool IsExplanationApproximate(int index)
	{
		if (index < 0 || index >= this.entryCount)
			return false;

		return this.explanationApproximate[index];
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
					this.entryCount = i + 1;
					break;
				}
			}
			return;
		}

		this.entryCount = restoreFrom;

		for (var i = markerIndex; i < this.levelStarts.Length; ++i)
			this.levelStarts[i] = -1;
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
	}

	private void EnsureLevelStart(int level)
	{
		if (level < 0 || level >= this.levelStarts.Length)
			return;

		if (this.levelStarts[level] == -1)
			this.levelStarts[level] = this.entryCount;
	}

	private void AppendEntry(Entry entry, IList<BoundReason> explanation = null!,
		bool isApproximate = false)
	{
		if (this.entryCount >= this.entries.Length)
		{
			Array.Resize(ref this.entries, this.entries.Length * 2);
			Array.Resize(ref this.explanations, this.explanations.Length * 2);
			Array.Resize(ref this.explanationApproximate, this.explanationApproximate.Length * 2);
		}

		this.explanations[this.entryCount] = explanation;
		this.explanationApproximate[this.entryCount] = isApproximate;
		this.entries[this.entryCount++] = entry;
	}
}
