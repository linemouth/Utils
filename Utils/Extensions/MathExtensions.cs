using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public static class MathExtensions
    {
        public static string FormatTime(this double seconds, double decimals)
        {
            int minutes = (int)Math.Truncate(seconds / 60);
            seconds -= minutes * 60;
            int hours = (int)Math.Truncate(minutes / 60);
            minutes -= hours * 60;
            int days = (int)Math.Truncate(hours / 24);
            hours -= days * 24;
            int wholeSeconds = (int)Math.Truncate(seconds);
            double milliseconds = seconds - wholeSeconds;
            StringBuilder sb = new StringBuilder();
            if(days > 0)
            {
                sb.Append(days.ToString());
                sb.Append('d');
            }
            if(hours > 0)
            {
                sb.Append(hours.ToString("D2"));
                sb.Append(':');
            }
            if(minutes > 0)
            {
                sb.Append(minutes.ToString("D2"));
                sb.Append(':');
            }
            sb.Append(wholeSeconds.ToString("D2"));
            if(decimals > 0)
            {
                sb.Append('.');
                sb.Append(milliseconds.ToString($"F{decimals}").Substring(2));
            }
            return sb.ToString();
        }
    }
}
