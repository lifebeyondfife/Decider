# Decider v5.0.0

## New Features

### Configurable Variable Ordering (`IVariableOrderingHeuristic<T>`)

`StateInteger` now accepts an optional `IVariableOrderingHeuristic<int>` as a third constructor argument:

```csharp
var state = new StateInteger(variables, constraints, new FirstVariableOrdering());
```

Four built-in implementations are provided in `Decider.Csp.Integer`: `MostConstrainedOrdering` (default, unchanged behaviour), `RandomOrdering`, `FirstVariableOrdering`, `LastVariableOrdering`. Custom heuristics can be implemented against the `IVariableOrderingHeuristic<T>` interface in `Decider.Csp.BaseTypes`.

### `CumulativeInteger` Propagation Improvements

Stronger propagation with significantly reduced search space on scheduling problems:

- **Edge-finding** — detects and prunes based on ordering infeasibility at resource capacity
- **Not-first/not-last** — eliminates positions where a task cannot be scheduled first or last among a set
- **Energetic reasoning** — filters start times based on energy consumption in time windows

---

# Decider v4.0.0 - Major Release

## Breaking Changes

### ⚠️ Optimisation Now Minimises by Default

**This is a breaking change that affects all code using the `Search(IVariable<int> optimiseVar)` method.**

- **Previous behaviour (v1.x):** `state.Search(variable)` would **maximise** the variable
- **New behaviour (v2.x):** `state.Search(variable)` now **minimises** the variable

This change aligns Decider with the standard convention used by most constraint programming libraries (OR-Tools, Gecode, MiniZinc, etc.) where optimisation defaults to minimisation.

## New Features

### CumulativeInteger Global Constraint

Added support for resource-constrained scheduling problems with the new `CumulativeInteger` constraint.

```csharp
var starts = new List<VariableInteger>
{
    new VariableInteger("task1_start", 0, 20),
    new VariableInteger("task2_start", 0, 20),
    new VariableInteger("task3_start", 0, 20)
};

var durations = new List<int> { 3, 5, 4 };
var demands = new List<int> { 2, 3, 1 };
var capacity = 4;

var cumulative = new CumulativeInteger(starts, durations, demands, capacity);
```

The cumulative constraint ensures that at any time point, the sum of resource demands from overlapping tasks does not exceed the available capacity. This is essential for:
- Resource-Constrained Project Scheduling (RCPSP)
- Machine scheduling problems
- Workforce allocation problems

**Features:**
- Efficient compulsory part detection for strong propagation
- Time-indexed resource profile checking
- Domain filtering based on resource conflicts
- Supports IList<T> collections (follows project conventions)

### Example: RCPSP Solver

A complete Resource-Constrained Project Scheduling Problem example is now included:

```csharp
var rcpsp = new Decider.Example.Rcpsp.Rcpsp();
rcpsp.OptimiseMakespan();
Console.WriteLine($"Minimum makespan: {rcpsp.Solution["9"].InstantiatedValue}");
```
