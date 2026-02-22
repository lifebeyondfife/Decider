(*
  Copyright © Tobias Burger 2021

  This file is part of Decider.
*)
open System
open Decider.Csp.Integer
open Decider.Csp.BaseTypes

let solve rolls rollers =
    let (==) left right = ExpressionInteger.(=)(left, right)
    let expr i = ExpressionInteger(i)

    let kickboards = VariableInteger("kickboards", 0, rollers)
    let cityrollers = VariableInteger("cityrollers", 0, rollers)

    let constraints: IConstraint list = [
        ConstraintInteger(kickboards + cityrollers == expr rollers)
        ConstraintInteger(kickboards * expr 3 + cityrollers * expr 2 == expr rolls)
    ]

    let variables: IVariable<int> list = [
        kickboards
        cityrollers
    ]

    let state = StateInteger(variables, constraints, ClauseLearningEnabled = false)
    match state.Search() with
    | StateOperationResult.Solved ->
        printfn "Runtime:\t\t\t%A" state.Runtime
        printfn "Backtracks\t\t\t%A" state.Backtracks
        [ kickboards.Value, cityrollers.Value ]
    | _ ->
        [ 0, 0 ]

[<EntryPoint>]
let main argv =
    let solution = solve 37 15
    printfn "[(Kickboards, Cityrollers)]:\t%A" solution
    0
