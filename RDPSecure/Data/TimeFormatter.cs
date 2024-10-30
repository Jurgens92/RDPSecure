using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDPSecure.Data
{
    public static class TimeFormatter
    {
        public static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalDays >= 365)
            {
                int years = (int)(duration.TotalDays / 365);
                return years == 1 ? "1 Year" : $"{years} Years";
            }

            if (duration.TotalDays >= 1)
            {
                int days = (int)duration.TotalDays;
                return days == 1 ? "1 Day" : $"{days} Days";
            }

            if (duration.TotalHours >= 1)
            {
                int hours = (int)duration.TotalHours;
                return hours == 1 ? "1 Hour" : $"{hours} Hours";
            }

            if (duration.TotalMinutes >= 1)
            {
                int minutes = (int)duration.TotalMinutes;
                return minutes == 1 ? "1 Minute" : $"{minutes} Minutes";
            }

            return "Less than a minute";
        }
    }
}
