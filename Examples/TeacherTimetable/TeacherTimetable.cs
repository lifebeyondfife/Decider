/*
  Copyright © Iain McDonald 2010-2022
  
  This file is part of Decider.
*/
using System;
using System.Collections.Generic;
using System.Linq;

using Decider.Csp.BaseTypes;
using Decider.Csp.Integer;

namespace Decider.Example.TeacherTimetable;

public static class TeacherTimetable
{
	public static void Main()
	{
		#region Model

		var numberOfTeachers = 3;

		var hours = Enumerable.Range(0, 8).ToList();

		var monday = new List<VariableInteger>();
		var tuesday = new List<VariableInteger>();
		var wednesday = new List<VariableInteger>();
		var thursday = new List<VariableInteger>();
		var friday = new List<VariableInteger>();
		var saturday = new List<VariableInteger>();

		foreach (var period in hours)
		{
			monday.Add(new VariableInteger(string.Format("Monday {0}", period), 1, numberOfTeachers));
			tuesday.Add(new VariableInteger(string.Format("Tuesday {0}", period), 1, numberOfTeachers));
			wednesday.Add(new VariableInteger(string.Format("Wednesday {0}", period), 1, numberOfTeachers));
			thursday.Add(new VariableInteger(string.Format("Thursday {0}", period), 1, numberOfTeachers));
			friday.Add(new VariableInteger(string.Format("Friday {0}", period), 1, numberOfTeachers));

			if (period < 5)
				saturday.Add(new VariableInteger(string.Format("Saturday {0}", period), 1, numberOfTeachers));
		}

		var weekdays = new[] { monday, tuesday, wednesday, thursday, friday }.ToList();
		var days = new[] { monday, tuesday, wednesday, thursday, friday, saturday }.ToList();
		var week = days.SelectMany(x => x).ToList();

		#endregion

		#region Constraints

		var constraints = new List<IConstraint>();

		// No teacher teaches more than 5 hours per weekday
		foreach (var teacher in Enumerable.Range(1, numberOfTeachers))
		{
			foreach (var day in weekdays)
			{
				constraints.Add(new ConstraintInteger(day.
					Select(x => x == teacher).
					Aggregate((x, y) => x + y) <= 5));
			}
		}

		// No teacher teaches for more than two consecutive hours
		foreach (var day in days)
		{
			for (var window = 0; window < day.Count - 2; ++window)
			{
				var threeHourWindow = day.Skip(window).Take(3).ToList();

				constraints.Add(new ConstraintInteger(
					threeHourWindow[0] != threeHourWindow[1] |
					threeHourWindow[0] != threeHourWindow[2] |
					threeHourWindow[1] != threeHourWindow[2])
				);
			}
		}

		// No teacher teaches more than 27 hours per week
		foreach (var teacher in Enumerable.Range(1, numberOfTeachers))
		{
			constraints.Add(new ConstraintInteger(week.
				Select(x => x == teacher).
				Aggregate((x, y) => x + y) <= 27));
		}

		#endregion

		#region Search

		var state = new StateInteger(week, constraints);
		state.ClauseLearningEnabled = false;
		state.Search();

		foreach (var period in week)
		{
			Console.WriteLine("{0}: {1}", period.Name, period.Value);
		}

		Console.WriteLine("Runtime:\t{0}\nBacktracks:\t{1}\n", state.Runtime, state.Backtracks);

		#endregion
	}
}
