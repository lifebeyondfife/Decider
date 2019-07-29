/*
  Copyright © Iain McDonald 2010-2019
  
  This file is part of Decider.
*/
using System;
using System.Collections.Generic;
using System.Linq;

using Decider.Csp.BaseTypes;
using Decider.Csp.Integer;

namespace Decider.Example.TeacherTimetable
{
	public static class TeacherTimetable
	{
		public static void Main()
		{
			#region Model

			var numberOfTeachers = 3;

			var hours = Enumerable.Range(0, 8).ToList();

			var Monday = new List<VariableInteger>();
			var Tuesday = new List<VariableInteger>();
			var Wednesday = new List<VariableInteger>();
			var Thursday = new List<VariableInteger>();
			var Friday = new List<VariableInteger>();
			var Saturday = new List<VariableInteger>();

			foreach (var period in hours)
			{
				Monday.Add(new VariableInteger(string.Format("Monday {0}", period), 1, numberOfTeachers));
				Tuesday.Add(new VariableInteger(string.Format("Tuesday {0}", period), 1, numberOfTeachers));
				Wednesday.Add(new VariableInteger(string.Format("Wednesday {0}", period), 1, numberOfTeachers));
				Thursday.Add(new VariableInteger(string.Format("Thursday {0}", period), 1, numberOfTeachers));
				Friday.Add(new VariableInteger(string.Format("Friday {0}", period), 1, numberOfTeachers));

				if (period < 5)
					Saturday.Add(new VariableInteger(string.Format("Saturday {0}", period), 1, numberOfTeachers));
			}

			var Weekdays = new[] { Monday, Tuesday, Wednesday, Thursday, Friday }.ToList();
			var Days = new[] { Monday, Tuesday, Wednesday, Thursday, Friday, Saturday }.ToList();
			var Week = Days.SelectMany(x => x).ToList();

			#endregion

			#region Constraints

			var constraints = new List<IConstraint>();

			// No teacher teaches more than 5 hours per weekday
			foreach (var teacher in Enumerable.Range(1, numberOfTeachers))
			{
				foreach (var day in Weekdays)
				{
					constraints.Add(new ConstraintInteger(day.
						Select(x => x == teacher).
						Aggregate((x, y) => x + y) <= 5));
				}
			}

			// No teacher teaches for more than two consecutive hours
			foreach (var day in Days)
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
				constraints.Add(new ConstraintInteger(Week.
					Select(x => x == teacher).
					Aggregate((x, y) => x + y) <= 27));
			}

			#endregion

			#region Search

			IState<int> state = new StateInteger(Week, constraints);

			state.StartSearch(out StateOperationResult searchResult);

			foreach (var period in Week)
			{
				Console.WriteLine("{0}: {1}", period.Name, period.Value);
			}

			Console.WriteLine("Runtime:\t{0}\nBacktracks:\t{1}\n", state.Runtime, state.Backtracks);

			#endregion
		}
	}
}
