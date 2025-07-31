using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace BlazorAI.Plugins
{
	public class TimePlugin
	{

		[KernelFunction("get_time")]
		[Description("Gets the current system Date and Time and returns it to the caller")]
		[return: Description("The current date and time.")]
		public DateTime GetCurrentDateTime()
		{
			return DateTime.Now;
		}

		[KernelFunction("get_year")]
		[Description("Gets the year from a date passed in as a parameter")]
		[return: Description("The year from the date.")]
		public int GetYear(DateTime date)
		{
			return date.Year;
		}

		[KernelFunction("get_month")]
		[Description("Gets the month from a date passed in as a parameter")]
		[return: Description("The month from the date.")]
		public int GetMonth(DateTime date)
		{
			return date.Month;
		}

		[KernelFunction("get_day_of_week")]
		[Description("Gets the day of the week from a date passed in as a parameter")]
		[return: Description("The day of the week from the date.")]
		public DayOfWeek GetDayOfWeek(DateTime date)
		{
			return date.DayOfWeek;
		}

	}
}
