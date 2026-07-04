# Decider - Claude Code Notes

## Tools
- calibration/probe runs are allowed but must always carry a hard per-run timeout and a stated total budget for the sweep; full BenchmarkDotNet runs (multi-iteration, statistical) remain user-triggered, since they're the expensive final measurement, not the exploration tool

## Coding Conventions
- Avoid nested code as much as possible; return early
- Always use explicit visibility identifiers (`private`, `public`, `internal`, `protected`) for all members
- For list collections of type T, prefer the type declaration IList<T>, and the implementation List<T>, over arrays i.e. T[]
- Use properties rather that fields and accessors e.g. `public int MyInteger { get; private set; }` rather than `private int myInteger` and `public int MyInteger { get { return this.myInteger; } }`
- For iterating collections, prefer `foreach (var item in items)` than a standard for loop

## Deploy
- To publish to nuget, update the Version XML value in `Integer/Csp.csproj` with the appropriate SEMVER increment

## Clean-up
- If you modify a file, update the year at the top of the file if necessary
- If you run a performance test, update the table in `Performance/README.md`
