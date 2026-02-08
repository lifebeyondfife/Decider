/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System;
using System.Collections.Generic;

using Decider.Csp.BaseTypes;

namespace Decider.Csp.Integer;

internal class DomainTrail
{
	private struct Change
	{
		internal int VariableId;
		internal int ArrayIndex;
		internal uint OldBits;
		internal int OldLowerBound;
		internal int OldUpperBound;
		internal int OldSize;
		internal int OldGeneration;

		internal Change(int variableId, int arrayIndex, uint oldBits, int oldLowerBound,
			int oldUpperBound, int oldSize, int oldGeneration)
		{
			this.VariableId = variableId;
			this.ArrayIndex = arrayIndex;
			this.OldBits = oldBits;
			this.OldLowerBound = oldLowerBound;
			this.OldUpperBound = oldUpperBound;
			this.OldSize = oldSize;
			this.OldGeneration = oldGeneration;
		}
	}

	private Change[] changes;
	private int changeCount;
	private int[] depthStarts;

	internal DomainTrail(int maxVariables, int estimatedCapacity)
	{
		this.changes = new Change[estimatedCapacity];
		this.changeCount = 0;
		this.depthStarts = new int[maxVariables * 100];
		for (var i = 0; i < this.depthStarts.Length; ++i)
			this.depthStarts[i] = -1;
		this.depthStarts[0] = 0;
	}

	internal void RecordChange(int variableId, int arrayIndex, uint oldBits,
		int oldLowerBound, int oldUpperBound, int oldSize, int oldGeneration, int depth)
	{
		if (this.changeCount >= this.changes.Length)
			Array.Resize(ref this.changes, this.changes.Length * 2);

		if (depth >= 0 && depth < this.depthStarts.Length && this.depthStarts[depth] == -1)
			this.depthStarts[depth] = this.changeCount;

		this.changes[this.changeCount++] = new Change(variableId, arrayIndex, oldBits,
			oldLowerBound, oldUpperBound, oldSize, oldGeneration);
	}

	internal void Backtrack(int toDepth, IList<IVariable<int>> variables)
	{
		var markerIndex = toDepth + 1;
		if (markerIndex < 0 || markerIndex >= this.depthStarts.Length)
			return;

		var restoreFrom = this.depthStarts[markerIndex];
		if (restoreFrom == -1)
			return;

		for (var i = this.changeCount - 1; i >= restoreFrom; --i)
		{
			var change = this.changes[i];
			var variable = (VariableInteger)variables[change.VariableId];
			var domain = (DomainBinaryInteger)variable.BaseDomain;

			domain.RestoreBits(change.ArrayIndex, change.OldBits);
			domain.SetBounds(change.OldLowerBound, change.OldUpperBound, change.OldSize);
			variable.RestoreGeneration(change.OldGeneration);
		}

		this.changeCount = restoreFrom;

		for (var i = markerIndex; i < this.depthStarts.Length; ++i)
			this.depthStarts[i] = -1;
	}
}
