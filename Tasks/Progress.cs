using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Utils
{
    public class Progress
    {
        public string Description { get; set; }
        public string CurrentItem { get; set; } = "";
        public long Total { get; set; }
        public long Value
        {
            get => _value; set
            {
                _value = value;
                UpdateFilter();
            }
        }
        public bool UsePercent { get; set; } = false;
        public bool IsIntegral { get; set; } = false;
        public double OrderScale { get; set; } = 0;
        public string Suffix { get; set; } = "";
        public Task Task { get; private set; } = null;
        public double Rate { get; private set; } = 0;

        private static readonly Dictionary<int, string> orderPrefixes = new Dictionary<int, string> { { -5, "f" }, { -4, "p" }, { -3, "n" }, { -2, "u" }, { -1, "m" }, { 0, "" }, { 1, "k" }, { 2, "M" }, { 3, "G" }, { 4, "T" }, { 5, "P" } };
        private double filterConstant = 0.5;
        private Stopwatch filterTimer;
        private long lastFilterValue = 0;
        private long _value = 0;
        private Stopwatch _stopwatch = null;
        private int consoleRow = int.MinValue;
        private bool newLine = false;

        public Progress(string description, bool usePercent = true, long total = -1, long value = 0)
        {
            Description = description;
            Total = total;
            _value = value;
            UsePercent = usePercent;
        }
        public Progress(string description, bool usePercent, string suffix, long total = -1, long value = 0) : this(description, usePercent, total, value) => Suffix = suffix;
        public Progress(string description, double orderScale, bool isIntegral = false, long total = -1, long value = 0) : this(description, false, total, value)
        {
            OrderScale = orderScale;
            IsIntegral = isIntegral;
        }
        public Progress(string description, double orderScale, string suffix, bool isIntegral = false, long total = -1, long value = 0) : this(description, orderScale, isIntegral, total, value) => Suffix = suffix;
        public static Progress Run(Action<Progress> action, string description, bool usePercent = false, long total = -1, long value = 0, Progress progress = null) => Run(action, description, usePercent, "", total, value, progress);
        public static Progress Run(Action<Progress> action, string description, bool usePercent, string suffix, long total = -1, long value = 0, Progress progress = null)
        {
            if(progress == null)
            {
                progress = new Progress(description, usePercent, suffix, total, value);
            }
            else
            {
                progress.Description = description;
                progress.CurrentItem = "";
                progress.Total = total;
                progress.Value = value;
                progress.UsePercent = usePercent;
                progress.IsIntegral = false;
                progress.OrderScale = 0;
                progress.Suffix = suffix;
                progress.Rate = 0;
            }
            progress.Run(action);
            return progress;
        }
        public static Progress Run(Action<Progress> action, string description, double orderScale, bool isIntegral = false, long total = -1, long value = 0, Progress progress = null) => Run(action, description, orderScale, "", isIntegral, total, value, progress);
        public static Progress Run(Action<Progress> action, string description, double orderScale, string suffix, bool isIntegral = false, long total = -1, long value = 0, Progress progress = null)
        {
            if(progress == null)
            {
                progress = new Progress(description, orderScale, suffix, isIntegral, total, value);
            }
            else
            {
                progress.Description = description;
                progress.CurrentItem = "";
                progress.Total = total;
                progress.Value = value;
                progress.UsePercent = false;
                progress.IsIntegral = isIntegral;
                progress.OrderScale = orderScale;
                progress.Suffix = suffix;
                progress.Rate = 0;
            }
            progress.Run(action);
            return progress;
        }
        public async void Run(Action<Progress> action)
        {
            if(_stopwatch == null)
            {
                _stopwatch = Stopwatch.StartNew();
            }
            else
            {
                _stopwatch.Restart();
            }
            Task = Task.Run(() => action(this));
            await Task;
        }
        public override string ToString()
        {
            string buffer = UsePercent ? GetPercentString() : GetFractionString();
            buffer = buffer.AddIfNotNull(GetRateString(), " @");
            buffer = buffer.AddIfNotNull(GetTimeString(), ", ");
            buffer = Description.PadRight(30).AddIfNotNull(buffer, " (", ")");
            buffer = buffer.AddIfNotNull(CurrentItem, " ");
            return buffer;
        }
        public void Print()
        {
            if(newLine)
            {
                newLine = false;
                PrintLine();
            }
            else
            {
                string buffer = ToString();
                try
                {
                    if(consoleRow == int.MinValue)
                    {
                        consoleRow = Console.CursorTop;
                    }
                    int width = Console.WindowWidth - 1;
                    int rows = Console.CursorTop - consoleRow + 1;
                    Console.CursorTop = consoleRow;
                    Console.CursorLeft = 0;
                    string clearString = new string(' ', width);
                    for(int i = 0; i < rows; ++i)
                    {
                        Console.WriteLine(clearString);
                    }
                    Console.CursorTop = consoleRow;
                    Console.CursorLeft = 0;
                    Console.Write(buffer);
                }
                catch
                {
                    Console.Write($"\r{buffer}    ");
                }
            }
        }
        public void PrintLine()
        {
            Print();
            Console.WriteLine("");
        }
        public bool Monitor(long timeoutMilliseconds = long.MaxValue)
        {
            try
            {
                int consoleRow = Console.CursorTop;
            }
            catch { }

            Stopwatch timeoutStopwatch = Stopwatch.StartNew();
            while(!Task.IsCompleted && timeoutStopwatch.ElapsedMilliseconds < timeoutMilliseconds)
            {
                Print();
                Task.Wait(50);
            }
            PrintLine();
            return Task.IsCompleted;
        }
        public void SetNewLine()
        {
            newLine = true;
        }

        private string GetTimeString() => _stopwatch == null ? null : $"{_stopwatch.Elapsed.TotalSeconds:F2}s";
        private string GetFractionString() => Total >= 0 ? (Value >= 0 ? $"{FormatValue(Value)}/{FormatValue(Total)}" : FormatValue(Total)) : (Value >= 0 ? FormatValue(Value) : "");
        private string GetPercentString() => Value >= 0 && Total > 0 ? $"{(Value * 100.0 / Total).ToString($"F2")}%" : GetFractionString();
        private string GetRateString()
        {
            if(UsePercent || string.IsNullOrWhiteSpace(Suffix))
            {
                return "";
            }
            else
            {
                UpdateFilter();
                return $"{FormatValue(Rate)}/s";
            }
        }
        private string FormatValue(long value) => OrderScale == 0 || value == 0 ? $"{value}{Suffix}" : FormatValue((double)value);
        private string FormatValue(double value) => $"{value.Format(3, OrderScale, true, IsIntegral)}{Suffix}";
        private void UpdateFilter()
        {
            lock(this)
            {
                if(filterTimer == null)
                {
                    filterTimer = Stopwatch.StartNew();
                    lastFilterValue = 0;
                }
                else if(filterTimer.ElapsedMilliseconds > 50)
                {
                    double dt = filterTimer.Elapsed.TotalSeconds;
                    filterTimer.Restart();
                    
                    long difference = Value - lastFilterValue;
                    lastFilterValue = Value;
                    
                    double rate = difference / dt;
                    Rate = Math.Lerp(rate, Rate, Math.Pow(filterConstant, dt));
                }
            }
        }
    }
}
