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

### Example Output

**Decider Benchmarks:**
```
| Method                | BoardSize | Mean     | Error   | StdDev  | Backtracks | Gen0     | Allocated |
|---------------------- |---------- |---------:|--------:|--------:|-----------:|---------:|----------:|
| SolveNQueens          | 8         | 12.34 ms | 0.12 ms | 0.11 ms |     15,720 | 500.0000 |   4.12 MB |
| SolveNQueens          | 10        | 45.67 ms | 0.34 ms | 0.29 ms |    341,318 | 800.0000 |  12.45 MB |
| SolveLeagueGeneration | -         | 745.9 ms | 2.35 ms | 2.09 ms |  1,234,567 | 270000.0 |   2.11 GB |
```

**OR-Tools Benchmarks:**
```
| Method       | BoardSize | Mean     | Error   | StdDev  | Conflicts | Branches  | Gen0     | Allocated |
|------------- |---------- |---------:|--------:|--------:|----------:|----------:|---------:|----------:|
| SolveNQueens | 8         | 10.25 ms | 0.08 ms | 0.07 ms |    12,340 |    24,680 | 400.0000 |   3.45 MB |
| SolveNQueens | 10        | 38.12 ms | 0.29 ms | 0.25 ms |   285,902 |   571,804 | 650.0000 |  10.23 MB |
```

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
