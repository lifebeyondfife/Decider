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
dotnet run -c Release -- --filter "*OrToolsNQueensBenchmark*"
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

### OrToolsNQueensBenchmark
- **Problem**: N-Queens using Google OR-Tools CP-SAT solver
- **Search**: Enumerate all solutions
- **Parameters**: Board sizes 8, 10, 12
- **Metrics**: Execution time, memory allocations, conflicts, branches
- **Purpose**: Direct competitive comparison with Decider
  - **Conflicts**: Analogous to Decider's backtracks
  - **Branches**: Total search nodes explored

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
| Method       | BoardSize | Mean        | Error      | StdDev    | Backtracks | Gen0         | Gen1       | Gen2      | Allocated   |
|------------- |---------- |------------:|-----------:|----------:|-----------:|-------------:|-----------:|----------:|------------:|
| SolveNQueens | 8         |    11.36 ms |   1.951 ms |  0.107 ms |      1,029 |    4765.6250 |   515.6250 |  500.0000 |    36.36 MB |
| SolveNQueens | 10        |   200.58 ms |   5.170 ms |  0.283 ms |     14,036 |   73666.6667 |  1666.6667 |  333.3333 |   589.75 MB |
| SolveNQueens | 12        | 5,098.02 ms | 183.319 ms | 10.048 ms |    278,540 | 1841000.0000 | 30000.0000 | 5000.0000 | 14667.94 MB |
```

**Decider LeagueGeneration:**
```
| Method                | Mean     | Error   | StdDev  | Backtracks | Gen0        | Gen1      | Gen2      | Allocated |
|---------------------- |---------:|--------:|--------:|-----------:|------------:|----------:|----------:|----------:|
| SolveLeagueGeneration | 761.9 ms | 7.78 ms | 0.43 ms |      6,249 | 259000.0000 | 2000.0000 | 1000.0000 |   2.07 GB |
```

**OR-Tools NQueens:**
```
| Method       | BoardSize | Mean        | Error      | StdDev   | Conflicts | Branches | Allocated |
|------------- |---------- |------------:|-----------:|---------:|----------:|---------:|----------:|
| SolveNQueens | 8         |    12.75 ms |   0.111 ms | 0.006 ms |       650 |    7,238 |  40.68 KB |
| SolveNQueens | 10        |   340.93 ms |  16.757 ms | 0.918 ms |    10,902 |   74,184 |  68.52 KB |
| SolveNQueens | 12        | 4,979.17 ms | 125.127 ms | 6.859 ms |   133,379 |  669,397 |  96.77 KB |
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
