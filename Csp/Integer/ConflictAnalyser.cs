/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System.Collections.Generic;

using Decider.Csp.BaseTypes;

namespace Decider.Csp.Integer;

internal static class ConflictAnalyser
{
	internal static bool Analyse(PropagationTrail trail, IList<BoundReason> conflictExplanation,
		int currentLevel, IList<IConstraint> constraints, IList<IVariable<int>> variables,
		out BoundReason[] learnedClause, out int assertionLevel)
	{
		learnedClause = null!;
		assertionLevel = 0;

		if (conflictExplanation.Count == 0 || currentLevel <= 0)
			return false;

		var nogood = new Dictionary<(int, bool), BoundReason>();

		foreach (var reason in conflictExplanation)
		{
			var key = (reason.VariableIndex, reason.IsLowerBound);
			if (!nogood.ContainsKey(key))
				nogood[key] = reason;
			else
				StrengthenLiteral(nogood, key, reason);
		}

		var currentLevelCount = CountAtLevel(nogood, trail, currentLevel);

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
			if (!nogood.ContainsKey(key))
				continue;

			var foundLevel = FindDecisionLevel(trail, nogood[key]);
			if (foundLevel != currentLevel)
				continue;

			if (trail.IsExplanationApproximate(i))
				return false;

			nogood.Remove(key);

			var explanation = GetExplanationFromEntry(i, trail, constraints, variables);
			foreach (var antecedent in explanation)
			{
				var antKey = (antecedent.VariableIndex, antecedent.IsLowerBound);
				if (!nogood.ContainsKey(antKey))
					nogood[antKey] = antecedent;
				else
					StrengthenLiteral(nogood, antKey, antecedent);
			}

			currentLevelCount = CountAtLevel(nogood, trail, currentLevel);
		}

		var literals = new List<BoundReason>();
		assertionLevel = 0;

		foreach (var kvp in nogood)
		{
			var original = kvp.Value;
			var negated = NegateLiteral(original);
			literals.Add(negated);

			var level = FindDecisionLevel(trail, original);
			if (level == currentLevel)
				continue;

			if (level > assertionLevel)
				assertionLevel = level;
		}

		if (literals.Count == 0)
			return false;

		learnedClause = literals.ToArray();
		return true;
	}

	private static int CountAtLevel(Dictionary<(int, bool), BoundReason> nogood,
		PropagationTrail trail, int level)
	{
		var count = 0;
		foreach (var kvp in nogood)
		{
			if (FindDecisionLevel(trail, kvp.Value) == level)
				++count;
		}
		return count;
	}

	private static int FindDecisionLevel(PropagationTrail trail, BoundReason literal)
	{
		for (var i = 0; i < trail.Count; ++i)
		{
			ref var entry = ref trail.GetEntry(i);
			if (entry.VariableId != literal.VariableIndex || entry.IsLowerBound != literal.IsLowerBound)
				continue;

			if (literal.IsLowerBound && entry.NewBound >= literal.BoundValue)
				return entry.DecisionLevel;

			if (!literal.IsLowerBound && entry.NewBound <= literal.BoundValue)
				return entry.DecisionLevel;
		}

		return 0;
	}

	private static IList<BoundReason> GetExplanationFromEntry(int entryIndex, PropagationTrail trail,
		IList<IConstraint> constraints, IList<IVariable<int>> variables)
	{
		ref var entry = ref trail.GetEntry(entryIndex);

		var storedExplanation = trail.GetExplanation(entryIndex);
		if (storedExplanation != null)
			return storedExplanation;

		if (entry.ReasonKind == PropagationTrail.ReasonConstraint &&
			entry.ReasonIndex >= 0 && entry.ReasonIndex < constraints.Count)
		{
			var constraint = constraints[entry.ReasonIndex];
			if (constraint is IExplainableConstraint explainable)
			{
				var result = new List<BoundReason>();
				explainable.Explain(entry.VariableId, entry.IsLowerBound, entry.NewBound, result);
				return result;
			}
		}

		return new List<BoundReason>();
	}

	private static BoundReason NegateLiteral(BoundReason literal)
	{
		if (literal.IsLowerBound)
			return new BoundReason(literal.VariableIndex, false, literal.BoundValue - 1);

		return new BoundReason(literal.VariableIndex, true, literal.BoundValue + 1);
	}

	private static void StrengthenLiteral(Dictionary<(int, bool), BoundReason> nogood, (int, bool) key, BoundReason newLiteral)
	{
		var existing = nogood[key];
		if (newLiteral.IsLowerBound)
		{
			if (newLiteral.BoundValue > existing.BoundValue)
				nogood[key] = newLiteral;
		}
		else
		{
			if (newLiteral.BoundValue < existing.BoundValue)
				nogood[key] = newLiteral;
		}
	}
}
