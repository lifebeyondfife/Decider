/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System;

namespace Decider.Csp.Global;

internal class ThetaTree
{
	private long[] Envelope { get; set; }
	private long[] EnvEnergy { get; set; }
	private long[] SumEnergy { get; set; }
	private int LeafOffset { get; set; }
	private int TreeSize { get; set; }

	internal ThetaTree()
	{
		this.Envelope = Array.Empty<long>();
		this.EnvEnergy = Array.Empty<long>();
		this.SumEnergy = Array.Empty<long>();
	}

	internal long RootEnvelope => this.TreeSize >= 2 ? this.Envelope[1] : long.MinValue;
	internal long RootEnvEnergy => this.TreeSize >= 2 ? this.EnvEnergy[1] : 0;

	internal void Reset(int eventCount)
	{
		if (eventCount <= 0)
		{
			this.TreeSize = 0;
			this.LeafOffset = 0;
			return;
		}

		var leafCount = 1;
		while (leafCount < eventCount)
			leafCount <<= 1;

		this.LeafOffset = leafCount;
		this.TreeSize = leafCount << 1;

		if (this.Envelope.Length < this.TreeSize)
		{
			this.Envelope = new long[this.TreeSize];
			this.EnvEnergy = new long[this.TreeSize];
			this.SumEnergy = new long[this.TreeSize];
		}

		for (var i = 0; i < this.TreeSize; ++i)
		{
			this.Envelope[i] = long.MinValue;
			this.EnvEnergy[i] = 0;
			this.SumEnergy[i] = 0;
		}
	}

	internal void Activate(int eventIndex, long initialEnvelope, long energy)
	{
		var leaf = this.LeafOffset + eventIndex;
		this.Envelope[leaf] = initialEnvelope;
		this.EnvEnergy[leaf] = energy;
		this.SumEnergy[leaf] = energy;

		RefreshAncestors(leaf);
	}

	internal void Deactivate(int eventIndex)
	{
		var leaf = this.LeafOffset + eventIndex;
		this.Envelope[leaf] = long.MinValue;
		this.EnvEnergy[leaf] = 0;
		this.SumEnergy[leaf] = 0;

		RefreshAncestors(leaf);
	}

	private void RefreshAncestors(int node)
	{
		node >>= 1;

		while (node >= 1)
		{
			var left = node << 1;
			var right = left | 1;

			var leftEnv = this.Envelope[left];
			var leftEnvE = this.EnvEnergy[left];
			var leftSumE = this.SumEnergy[left];
			var rightEnv = this.Envelope[right];
			var rightSumE = this.SumEnergy[right];

			if (leftEnv == long.MinValue)
			{
				this.Envelope[node] = rightEnv;
				this.EnvEnergy[node] = this.EnvEnergy[right];
			}
			else if (rightEnv == long.MinValue || leftEnv + rightSumE >= rightEnv)
			{
				this.Envelope[node] = leftEnv + rightSumE;
				this.EnvEnergy[node] = leftEnvE + rightSumE;
			}
			else
			{
				this.Envelope[node] = rightEnv;
				this.EnvEnergy[node] = this.EnvEnergy[right];
			}

			this.SumEnergy[node] = leftSumE + rightSumE;

			node >>= 1;
		}
	}
}
