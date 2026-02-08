Decider
=======

![main ci pipeline](https://github.com/lifebeyondfife/decider/actions/workflows/main.yml/badge.svg)
![nuget package](https://img.shields.io/nuget/v/Decider)
![nuget package](https://img.shields.io/nuget/dt/Decider)


An Open Source .Net Constraint Programming Solver


Installation
------------

Install using nuget for .Net 10.0 (.Net 8.0, .Net Standard 2.0 and 2.1 also supported)

     dotnet add package Decider

Variables
---------

Create constrained integer variables

```csharp
var s = new VariableInteger("s", 0, 9);
var e = new VariableInteger("e", 0, 9);
var n = new VariableInteger("n", 0, 9);
var d = new VariableInteger("d", 0, 9);
var m = new VariableInteger("m", 1, 9);
var o = new VariableInteger("o", 0, 9);
var r = new VariableInteger("r", 0, 9);
var y = new VariableInteger("y", 0, 9);
var c0 = new VariableInteger("c0", 0, 1);
var c1 = new VariableInteger("c1", 0, 1);
var c2 = new VariableInteger("c2", 0, 1);
var c3 = new VariableInteger("c3", 0, 1);
```


Constraints
-----------

Define the constraints of your problem

```csharp
var constraints = new List<IConstraint>
{
    new AllDifferentInteger(new [] { s, e, n, d, m, o, r, y }),
    new ConstraintInteger(d + e == (10 * c0) + y),
    new ConstraintInteger(n + r + c0 == (10 * c1) + e),
    new ConstraintInteger(e + o + c1 == (10 * c2) + n),
    new ConstraintInteger(s + m + c2 == (10 * c3) + o),
    new ConstraintInteger(c3 == m)
};
```


Search
------

Find a solution using Decider's search routines

```csharp
var variables = new [] { c0, c1, c2, c3, s, e, n, d, m, o, r, y };
var state = new StateInteger(variables, constraints);
var searchResult = state.Search();

Console.WriteLine($"    {s} {e} {n} {d} ");
Console.WriteLine($"  + {m} {o} {r} {e} ");
Console.WriteLine("  ---------");
Console.WriteLine($"  {m} {o} {n} {e} {y} ");

Console.WriteLine($"Runtime:\t{state.Runtime}\nBacktracks:\t{state.Backtracks}\n");
```

Which results in

        9 5 6 7
      + 1 0 8 5
      ---------
      1 0 6 5 2

    Runtime:        00:00:00.0290211
    Backtracks:     85


Find All Solutions
------------------

Display all solutions to the n-queens problem

```csharp
var searchResult = state.SearchAllSolutions();

foreach (var solution in state.Solutions)
{
    for (var i = 0; i < variables.Length; ++i)
    {
        for (var j = 0; j < variables.Length; ++j)
            Console.Write(solution[i.ToString()].InstantiatedValue == j ? "Q " : ". ");

        Console.WriteLine();
    }
    Console.WriteLine();
}
```

Which results in

    Q . . . . . . .
    . . . . Q . . .
    . . . . . . . Q
    . . . . . Q . .
    . . Q . . . . .
    . . . . . . Q .
    . Q . . . . . .
    . . . Q . . . .

    Q . . . . . . .
    . . . . . Q . .
    . . . . . . . Q
    . . Q . . . . .
    . . . . . . Q .
    . . . Q . . . .
    . Q . . . . . .
    . . . . Q . . .


and a further ninety solutions.


Progress Reporting
------------------

For long-running searches, track progress through the search space with a callback

```csharp
state.OnProgress = progress =>
{
    var filled = (int)(progress * 50);
    Console.Write($"\r[{new string('#', filled)}{new string('-', 50 - filled)}] {progress:P1}  " +
        $"{state.Backtracks} backtracks, {state.Solutions.Count} solutions");
};

state.SearchAllSolutions();
Console.WriteLine();
```

Which displays a live updating progress bar

    [####################------------------------------] 40.2%  15234 backtracks, 42 solutions

The progress value represents the fraction of the combinatorial search space that has been explored. Progress reports occur every second by default, configurable via `ProgressInterval`.


Optimise
--------

Create an integer variable to optimise i.e. make as large as possible

```csharp
var optimise = new VariableInteger("optimalAnswer");
new ConstraintInteger(optimise == a + b + c + d + e + f + g + h)
```


Specify an upper time bound on how long you search, say, five minutes

```csharp
var timeout = 60 * 5;
var searchResult = state.Search(optimise, timeout);
Console.WriteLine($"Optimal answer found is ${state.OptimalSolution["optimalAnswer"]}");
```


Constrained Arrays
------------------

Index integer arrays with constrained integer variables

```csharp
var a = new VariableInteger("a", 0, 9);
var array = new ConstrainedArray(new int[] { 0, 23, 52, 62, 75, 73, 47, 20, 87, 27 });
    
var constraint = new ConstraintInteger(array[a] < 40)
```


F#
---

Thanks to contributor [@toburger](https://github.com/toburger), there's a simple example of using Decider in F#.

```fsharp
let constraints: IConstraint list = [
    ConstraintInteger(kickboards + cityrollers == expr rollers)
    ConstraintInteger(kickboards * expr 3 + cityrollers * expr 2 == expr rolls)
]
```
See [Examples/FSharp](https://github.com/lifebeyondfife/Decider/tree/main/Examples/FSharp) for full details.



More Examples?
--------------

Fork the repo to see more examples, some toy and some real world, of Decider in action.


Licence
-------

Released under the MIT licence, Decider is freely available for commercial use.


Author
------

I have a PhD in Constraint Programming and love turning NP-complete problems into CSPs. Visit my blog at https://lifebeyondfife.com/.
