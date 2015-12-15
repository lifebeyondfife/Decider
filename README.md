Decider
=======

An Open Source .Net Constraint Programming Solver


Installation
============

Install using nuget

    Install-Package Decider


Variables
---------

Created constrained integer _variables_

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

Define the _constraints_ of your problem

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

Find a solution using Decider's _search_ routines

```csharp
    StateOperationResult searchResult;
    state.StartSearch(out searchResult);
    
    if (searchResult == StateOperationResult.Solved)
    {
        Console.WriteLine("    {0} {1} {2} {3} ", s, e, n, d);
        Console.WriteLine("  + {0} {1} {2} {3} ", m, o, r, e);
        Console.WriteLine("  ---------");
        Console.WriteLine("  {0} {1} {2} {3} {4} \n", m, o, n, e, y);
    }
    
    Console.WriteLine("Runtime:\t{0}, state.Runtime);
    Console.WriteLine("Backtracks:\t{1}", state.Backtracks);
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

```csharp
    StateOperationResult searchResult;
    IList<IDictionary<string, IVariable<int>>> solutions;
    state.StartSearch(out searchResult, out solutions);

    foreach (var solution in solutions)
    {
        for (var i = 0; i < variables.Length; ++i)
        {
            for (var j = 0; j < variables.Length; ++j)
                Console.Write(solution[i.ToString(CultureInfo.CurrentCulture)].InstantiatedValue == j ? "Q" : ".");
            
            Console.WriteLine();
        }
        Console.WriteLine();
    }
```

Which results in

    Q.......
    ....Q...
    .......Q
    .....Q..
    ..Q.....
    ......Q.
    .Q......
    ...Q....
    
    Q.......
    .....Q..
    .......Q
    ..Q.....
    ......Q.
    ...Q....
    .Q......
    ....Q...

and ninety more solutions.


Optimise
--------

Create an integer variable to optimise

```csharp
    new ConstraintInteger(optimise == a + b + c + d + e + f + g + h)
```


Specify an upper time bound on how long you search, say, five minutes

```csharp
    var timeout = 60 * 5;
    StateOperationResult searchResult;
    var solution = default(IDictionary<string, IVariable<int>>);
    
    state.StartSearch(out searchResult, optimise, out solution, timeout);
```


Constrained Arrays
------------------

Index integer arrays with constained integer variables

```csharp
    var a = new VariableInteger("a", 0, 9);
    var array = new ConstrainedArray(new int[] { 0, 23, 52, 62, 75, 73, 47, 20, 87, 27 });
    
    var constraint = new ConstraintInteger(array[a] < 40)
```


More Examples?
--------------

Fork the repo to see more examples, some toy and some real world, of Decider in action.


Licence
-------

Released under the MIT licence, Decider is freely available for commercial use.


Author
------

I have a PhD in Constraint Programming and love turning NP-complete problems into CSPs. Visit my blog at http://lifebeyondfife.com/.
