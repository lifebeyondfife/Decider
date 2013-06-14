Decider
=======

An Open Source .Net Constraint Programming Solver


What is this?
=============

Constraint programming is a programming paradigm from the field of Artificial Intelligence. It's a combinatorial search technique used to solve NP-complete problems.


And once more in English, please...
-----------------------------------

Constraint Satisfaction Problems (CSPs) are problems that are hard to solve because there are an exponential number of combinations to try out. The archetypal example problem is <a href="https://en.wikipedia.org/wiki/Travelling_salesman_problem">The Travelling Salesperson</a>. There are many different techniques for solving these types of problem but one of the most general is to a Constraint Solver like Decider (the GitHub repo you're looking at). In its simplest form, a Constraint Solver takes a description of a problem and it works out a solution to it. The types of problem it can solve are generally logic puzzles, timetabling, scheduling and planning problems. Things like the logistics for balancing power levels across power plants, routes for delivery companies, schedules for factories, timetables for surgeries in hospitals, right down to making or solving Sudokus.


How does it work?
=================

Constraint Programming splits the problem solving into three distinct phases: modelling, constraints, and search.

Modelling
---------

Come up with a representation of what a solution to your problem looks like. For simple problems this could be an array of integers where each value in the array is a reference/index/lookup that allows you to describe a valid solution. Each unknown integer in the model is known as a *variable*. For example, a solution to The Travelling Salesperson could be represented by an array of integers which represents the ordered route of cities he/she must visit.

Constraints
-----------

The problem is described by adding constraints i.e. you rule out combinations from your model that aren't valid as a solution. Going back to The Travelling Salesperson problem, once one city has been visited, the salesperson cannot go back to that city again. If our solution model (as described above) is a list of the cities travelled to in order, we can specify this condition of not revisting cities by adding a constraint that states all the variables must be different to each other.

Search
------

Once the model and constraints have been specified, we have an exponential search space of combinations to try out that might yield a solution to our problem. The Constraint Solver navigates through the possibilities by using the constraints to narrow down the search performed as much as possible.


Example Problems
================

Decider comes with three console based examples:

 1. N-Queens. Given an N x N chessboard, place N Queens on it such that none of them can attack each other i.e. none of the Queens are in the same horizontal, vertical or diagnoal line.
 2. Phase Locked Loop. A real world problem to do with synching sampling frequencies when converting video streams. Given an input frequency and a desired output frequency, adjust the parameters of the loop model so that the output frequency of the first loop matches the input frequency of the second.
 3. SEND + MORE = MONEY. A simple arithmetic puzzle.


Future of Decider
=================

I've worked on Decider on and off (mostly off) for the past few years. I thought it was finally time to share it with the world. It has many issues that need improvement, however, without letting others see it there was a danger the project would stagnate and come to nothing. Planned improvements include:

 - Fixing the poor performance of the AllDifferent constraint by maintaining the data structures during search, rather than starting afresh at every node.
 - Looking at non-integer domain variables.
 - Parallelising the search routine for multiple cores.
 - Analytics for intra-search feedback i.e. how much of the search tree has been traversed?
 - Symmetric constraints (bit of a pipe dream this one, but it's a previous research area of mine).
 
