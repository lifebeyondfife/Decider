/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System;
using System.Collections.Generic;

using Decider.Csp.BaseTypes;

namespace Decider.Csp.Integer;

internal static class ConflictAnalyser
{
	internal static bool Analyse(PropagationTrail trail, IList<BoundReason> conflictExplanation,
		int currentLevel, Func<int, IList<BoundReason>> explanationProvider,
		out BoundReason[] learnedClause, out int assertionLevel, out bool isAsserting)
	{
		learnedClause = null!;
		assertionLevel = 0;
		isAsserting = false;

		if (conflictExplanation.Count == 0 || currentLevel <= 0)
			return false;

		var nogood = new Dictionary<(int, bool), BoundReason>();
		var currentLevelCount = 0;

		foreach (var reason in conflictExplanation)
			AddLiteral(nogood, trail, currentLevel, ref currentLevelCount, reason);

		if (currentLevelCount <= 0)
			return false;

		for (var i = trail.Count - 1; i >= 0 && currentLevelCount > 1; --i)
		{
			ref var entry = ref trail.GetEntry(i);
			if (entry.DecisionLevel != currentLevel)
				continue;

			if (entry.ReasonKind == PropagationTrail.ReasonDecision)
				continue;

			var key = (entry.VariableId, entry.IsLowerBound);
			if (!nogood.TryGetValue(key, out var resolved))
				continue;

			var foundLevel = trail.FindDecisionLevel(resolved.VariableIndex, resolved.IsLowerBound, resolved.BoundValue);
			if (foundLevel != currentLevel)
				continue;

			nogood.Remove(key);
			--currentLevelCount;

			var explanation = explanationProvider(i);

			foreach (var antecedent in explanation)
				AddLiteral(nogood, trail, currentLevel, ref currentLevelCount, antecedent);
		}

		var literals = new List<BoundReason>();
		assertionLevel = 0;

		foreach (var kvp in nogood)
		{
			var original = kvp.Value;
			var negated = NegateLiteral(original);
			literals.Add(negated);

			var level = trail.FindDecisionLevel(original.VariableIndex, original.IsLowerBound, original.BoundValue);
			if (level == currentLevel)
				continue;

			if (level > assertionLevel)
				assertionLevel = level;
		}

		if (literals.Count == 0)
			return false;

		learnedClause = literals.ToArray();
		isAsserting = currentLevelCount == 1;
		return true;
	}

	private static void AddLiteral(Dictionary<(int, bool), BoundReason> nogood,
		PropagationTrail trail, int currentLevel, ref int currentLevelCount, BoundReason literal)
	{
		var key = (literal.VariableIndex, literal.IsLowerBound);
		if (!nogood.TryGetValue(key, out var existing))
		{
			nogood[key] = literal;
			if (trail.FindDecisionLevel(literal.VariableIndex, literal.IsLowerBound, literal.BoundValue) == currentLevel)
				++currentLevelCount;
			return;
		}

		var isStronger = literal.IsLowerBound
			? literal.BoundValue > existing.BoundValue
			: literal.BoundValue < existing.BoundValue;

		if (!isStronger)
			return;

		var oldAtLevel = trail.FindDecisionLevel(existing.VariableIndex, existing.IsLowerBound, existing.BoundValue) == currentLevel;
		var newAtLevel = trail.FindDecisionLevel(literal.VariableIndex, literal.IsLowerBound, literal.BoundValue) == currentLevel;

		if (oldAtLevel && !newAtLevel)
			--currentLevelCount;
		else if (!oldAtLevel && newAtLevel)
			++currentLevelCount;

		nogood[key] = literal;
	}

	private static BoundReason NegateLiteral(BoundReason literal)
	{
		if (literal.IsLowerBound)
			return new BoundReason(literal.VariableIndex, false, literal.BoundValue - 1);

		return new BoundReason(literal.VariableIndex, true, literal.BoundValue + 1);
	}
}
