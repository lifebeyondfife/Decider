# Decider Performance Benchmarks

Performance testing infrastructure for Decider using BenchmarkDotNet, enabling A/B comparison between branches and competitive benchmarking against Google OR-Tools' CP-SAT solver.

## Running Benchmarks

### Run all benchmarks

```bash
dotnet run -c Release
```

### Run specific benchmark

```bash
dotnet run -c Release -- --filter "*NQueensBenchmark*"
dotnet run -c Release -- --filter "*LeagueGenerationBenchmark*"
dotnet run -c Release -- --filter "*RcpspBenchmark*"
dotnet run -c Release -- --filter "*FurnitureMovingBenchmark*"
dotnet run -c Release -- --filter "*OrToolsNQueensBenchmark*"
dotnet run -c Release -- --filter "*OrToolsFurnitureMovingBenchmark*"
dotnet run -c Release -- --filter "*OrToolsRcpspBenchmark*"
```

### Run with specific board size

```bash
dotnet run -c Release -- --filter "*NQueensBenchmark*" --job short --warmupCount 1 --iterationCount 3
```

## Benchmarks

### NQueensBenchmark
- **Problem**: N-Queens using `AllDifferent` + pairwise arithmetic inequality constraints
- **Search**: `SearchAllSolutions()` - finds all solutions
- **Parameters**: Board sizes 8, 10, 12
- **Metrics**: Execution time, memory allocations, backtracks

### LeagueGenerationBenchmark
- **Problem**: League generation with `AllDifferent` on cross-cutting overlapping groups
- **Search**: `Search()` - finds first solution
- **Parameters**: Fixed league size of 20 (~1.5s runtime)
- **Metrics**: Execution time, memory allocations, backtracks

### RcpspBenchmark
- **Problem**: Resource-Constrained Project Scheduling Problem (RCPSP) using `Cumulative` constraint + precedence constraints
- **Search**: `Search(makespan)` - finds optimal (minimum makespan) solution for PSPLib j30 instance (j3010_1)
- **Parameters**: PSPLib j30 instance with horizon scaled 10x (164 → 1640) to create large domains
- **Metrics**: Execution time, memory allocations, backtracks
- **Purpose**: Direct comparison with OrToolsRcpspBenchmark; large domains demonstrate bounds-based timetable filtering advantage

### FurnitureMovingBenchmark
- **Problem**: Furniture moving scheduling using `Cumulative` constraint, based on Marriott & Stukey: 'Programming with constraints', page 112f
- **Search**: `Search(makespan)` - finds optimal (minimum makespan) solution
- **Parameters**: 8 tasks with varying durations (10-30) and demands (1-4), capacity 4, horizon 160
- **Metrics**: Execution time, memory allocations, backtracks
- **Purpose**: Direct comparison with OrToolsFurnitureMovingBenchmark

### OrToolsNQueensBenchmark
- **Problem**: N-Queens using Google OR-Tools CP-SAT solver
- **Search**: Enumerate all solutions
- **Parameters**: Board sizes 8, 10, 12
- **Metrics**: Execution time, memory allocations, conflicts, branches
- **Purpose**: Direct competitive comparison with Decider
  - **Conflicts**: Analogous to Decider's backtracks
  - **Branches**: Total search nodes explored

### OrToolsFurnitureMovingBenchmark
- **Problem**: Furniture moving scheduling using OR-Tools ConstraintSolver `Cumulative` constraint
- **Search**: Minimize makespan using fail-first heuristic (`CHOOSE_MIN_SIZE` / `ASSIGN_MIN_VALUE`)
- **Parameters**: Same 8 tasks as FurnitureMovingBenchmark
- **Metrics**: Execution time, memory allocations, failures, branches
- **Purpose**: Direct competitive comparison with Decider's `Cumulative` constraint
  - **Note**: `CHOOSE_MIN_SIZE` (smallest domain) is the closest OR-Tools equivalent to Decider's `DomWdegOrdering`; `ASSIGN_MIN_VALUE` (earliest start) is the standard approach for makespan minimisation in CP

### OrToolsRcpspBenchmark
- **Problem**: RCPSP using OR-Tools ConstraintSolver `Cumulative` constraint + precedence constraints
- **Search**: Minimize makespan using fail-first heuristic (`CHOOSE_MIN_SIZE` / `ASSIGN_MIN_VALUE`)
- **Parameters**: Same PSPLib j30 instance (j3010_1) and 10x horizon scaling as RcpspBenchmark
- **Metrics**: Execution time, memory allocations, failures, branches
- **Purpose**: Direct competitive comparison with Decider's RCPSP benchmark
  - **Note**: `CHOOSE_MIN_SIZE` (smallest domain) is the closest OR-Tools equivalent to Decider's `DomWdegOrdering`; `ASSIGN_MIN_VALUE` matches Decider's `LowestValueOrdering`

## Metrics Tracked

All benchmarks track the following metrics in the summary table:

1. **Execution Time** (mean, stddev) - BenchmarkDotNet default
2. **Memory Allocations** - Via `[MemoryDiagnoser]` attribute (Gen0, Gen1, Allocated)
3. **Backtracks** - Custom column showing `State.Backtracks` (Decider benchmarks only)
   - Distinguishes algorithmic improvements (fewer backtracks) from implementation optimizations
   - Flags fragile speedups (more backtracks but cheaper per-node cost)

### Latest Results

**Decider NQueens:**
```
| Method       | BoardSize | Mean         | Error      | StdDev     | Backtracks | Gen0        | Gen1       | Gen2      | Allocated  |
|------------- |---------- |-------------:|-----------:|-----------:|-----------:|------------:|-----------:|----------:|-----------:|
| SolveNQueens | 8         |     7.217 ms |  0.1387 ms |  0.1484 ms |        889 |   2757.8125 |   531.2500 |  500.0000 |   20.25 MB |
| SolveNQueens | 10        |   120.342 ms |  1.8667 ms |  1.7461 ms |     10,770 |  34800.0000 |  1200.0000 |  400.0000 |  278.55 MB |
| SolveNQueens | 12        | 2,915.387 ms | 31.6798 ms | 29.6333 ms |    210,151 | 804000.0000 | 31000.0000 | 6000.0000 | 6385.09 MB |
```

**Decider LeagueGeneration:**
```
| Method                | Mean     | Error     | StdDev    | Backtracks | Gen0      | Gen1      | Gen2     | Allocated |
|---------------------- |---------:|----------:|----------:|-----------:|----------:|----------:|---------:|----------:|
| SolveLeagueGeneration | 9.235 ms | 0.0171 ms | 0.0160 ms |          1 | 3234.3750 | 1250.0000 | 953.1250 |  70.81 MB |
```

**Decider FurnitureMoving:**
```
| Method               | Mean    | Error    | StdDev   | Backtracks | Gen0        | Allocated |
|--------------------- |--------:|---------:|---------:|-----------:|------------:|----------:|
| SolveFurnitureMoving | 2.142 s | 0.0193 s | 0.0180 s |  2,711,692 | 361000.0000 |   2.82 GB |
```

**Decider RcpspJ30:**
```
| Method                | Mean     | Error   | StdDev  | Backtracks | Gen0      | Gen1     | Gen2     | Allocated |
|---------------------- |---------:|--------:|--------:|-----------:|----------:|---------:|---------:|----------:|
| SolveRcpspJ30Instance | 263.7 ms | 2.76 ms | 2.58 ms |      3,266 | 4500.0000 | 500.0000 | 500.0000 |  42.09 MB |
```

**OR-Tools NQueens:**
```
| Method       | BoardSize | Mean        | Error     | StdDev   | Conflicts | Branches | Allocated |
|------------- |---------- |------------:|----------:|---------:|----------:|---------:|----------:|
| SolveNQueens | 8         |    13.78 ms |  0.069 ms | 0.064 ms |       650 |    7,238 |  40.68 KB |
| SolveNQueens | 10        |   368.35 ms |  2.463 ms | 2.304 ms |    10,902 |   74,184 |  68.52 KB |
| SolveNQueens | 12        | 5,357.23 ms | 11.495 ms | 9.599 ms |   133,379 |  669,397 |  96.77 KB |
```

**OR-Tools FurnitureMoving:**
```
| Method               | Mean     | Error   | StdDev  | Failures | Branches | Allocated |
|--------------------- |---------:|--------:|--------:|---------:|---------:|----------:|
| SolveFurnitureMoving | 947.4 ms | 3.99 ms | 3.54 ms |  211,664 |  423,322 |         - |
```

**OR-Tools RcpspJ30:**
```
| Method                | Mean     | Error   | StdDev  | Failures | Branches | Allocated |
|---------------------- |---------:|--------:|--------:|---------:|---------:|----------:|
| SolveRcpspJ30Instance | 603.1 ms | 0.71 ms | 0.67 ms |    6,201 |   12,311 |  97.91 KB |
```

> **Note:** OR-Tools allocation figures only reflect .NET managed heap usage (the C# interop layer). The solver itself is written in C++ and allocates on the native heap, which is not tracked by BenchmarkDotNet's `MemoryDiagnoser`. Direct memory comparisons between Decider and OR-Tools are therefore not meaningful.

**Search Metrics Explained:**
- **Backtracks** (Decider): Total backtracks during search - distinguishes algorithmic vs implementation improvements
- **Conflicts** (OR-Tools): Similar to backtracks - number of conflicts encountered
- **Branches** (OR-Tools): Total search nodes explored - indicates search tree size

These metrics allow you to:
- **Compare strategies**: OR-Tools vs Decider may explore different search trees
- **Identify improvement types**: Fewer backtracks/conflicts = better algorithm; same count but faster = better implementation
- **Spot fragile optimizations**: Faster but more backtracks/conflicts may regress on other problems

## Workflow

The intended workflow for A/B testing:

1. **Baseline**: Run benchmarks on `main` branch
   ```bash
   git checkout main
   dotnet run -c Release
   ```

2. **Compare**: Switch to working branch and run again
   ```bash
   git checkout feature-branch
   dotnet run -c Release
   ```

3. **Analyze**: BenchmarkDotNet automatically compares results and shows relative performance

## BenchmarkDotNet Features

BenchmarkDotNet provides:
- Statistical soundness with warmup, outlier detection, and multi-run aggregation
- Automatic baseline comparison
- Export to various formats (markdown, CSV, HTML, JSON)
- Integration with CI/CD systems
- Detailed diagnostics and analysis

## Notes

- Always run benchmarks in **Release** configuration for accurate results
- Multiple runs are performed automatically to ensure statistical significance
- Results are saved to `BenchmarkDotNet.Artifacts/` directory
- For reliable comparisons, ensure consistent system state (close other applications, disable turbo boost if needed)
