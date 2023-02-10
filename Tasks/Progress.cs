using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Utils
{
    public class Progress
    {
        public string Description { get; set; }
        public string CurrentItem { get; set; } = "";
        public long Total { get; set; }
        public long Value { get; set; }
        public double Rate { get; protected set; } = 0;
        public string Suffix { get; set; } = "";
        public double Elapsed => stopwatch == null ? 0 : stopwatch.Elapsed.TotalSeconds;
        public double Percent => Value >= 0 && Total > 0 ? 100.0 * Value / Total : 0;
        public bool UsePercent { get; set; } = false;
        public bool IsIntegral { get; set; } = false;
        public double OrderScale { get; set; } = 0;
        public Task Task
        {
            get => task;
            protected set
            {
                task = value;
                task.GetAwaiter().OnCompleted(HandleOnCompleted);
            }
        }
        public event Action OnUpdate;

        protected static readonly Dictionary<int, string> orderPrefixes = new Dictionary<int, string> { { -5, "f" }, { -4, "p" }, { -3, "n" }, { -2, "u" }, { -1, "m" }, { 0, "" }, { 1, "k" }, { 2, "M" }, { 3, "G" }, { 4, "T" }, { 5, "P" } };
        protected double filterConstant = 0.5;
        protected double lastFilterTime = 0;
        protected long lastFilterValue = 0;
        protected long value = 0;
        protected System.Timers.Timer updateTimer = new System.Timers.Timer(50);
        protected int consoleRow = int.MinValue;
        protected bool newLine = false;

        private Stopwatch stopwatch = null;
        private Task task = null;

        public Progress(string description, bool usePercent = true, long total = -1, long value = 0)
        {
            Description = description;
            Total = total;
            this.value = value;
            UsePercent = usePercent;
            updateTimer.Elapsed += OnUpdateTimerElapsed;
        }
        public Progress(string description, bool usePercent, string suffix, long total = -1, long value = 0) : this(description, usePercent, total, value) => Suffix = suffix;
        public Progress(string description, double orderScale, bool isIntegral = false, long total = -1, long value = 0) : this(description, false, total, value)
        {
            OrderScale = orderScale;
            IsIntegral = isIntegral;
        }
        public Progress(string description, double orderScale, string suffix, bool isIntegral = false, long total = -1, long value = 0) : this(description, orderScale, isIntegral, total, value) => Suffix = suffix;
        public static bool Monitor(IEnumerable<Progress> jobs, long timeoutMilliseconds = long.MaxValue)
        {
            int consoleRow = 0;
            try
            {
                consoleRow = Console.CursorTop;
            }
            catch { }

            Stopwatch timeoutStopwatch = Stopwatch.StartNew();
            bool complete = false;
            while(!complete && timeoutStopwatch.ElapsedMilliseconds < timeoutMilliseconds)
            {
                complete = jobs.All(t => t.Task.IsCompleted);

                string buffer = string.Join("\n", jobs.Select(progress => progress.ToString()));
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
                Thread.Sleep(50);
            }

            Console.WriteLine("");
            return jobs.All(t => t.Task.IsCompleted);
        }
        public async void Run(Action<Progress> action)
        {
            if(stopwatch == null)
            {
                stopwatch = Stopwatch.StartNew();
            }
            updateTimer.Start();
            if(Task == null)
            {
                Task = Task.Run(() => action(this));
            }
            else if(Task?.Status == TaskStatus.Created)
            {
                Task.Start();
            }
            await Task;
            stopwatch.Stop();
            updateTimer.Stop();
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
        public void Increment(string description)
        {
            ++Value;
            Description = description;
        }
        public void Set(long value)
        {
            Value = value;
        }
        public void Set(string description)
        {
            Description = description;
        }
        public void Set(long value, long total)
        {
            Value = value;
            Total = total;
        }
        public void Set(long value, string description)
        {
            Value = value;
            Description = description;
        }
        public void Set(long value, long total, string description)
        {
            Value = value;
            Total = total;
            Description = description;
        }
        public void SetNewLine() => newLine = true;

        protected string GetTimeString() => Elapsed.FormatTime(2);
        protected string GetFractionString() => Total >= 0 ? (Value >= 0 ? $"{FormatValue(Value)}/{FormatValue(Total)}" : FormatValue(Total)) : (Value >= 0 ? FormatValue(Value) : "");
        protected string GetPercentString() => Value >= 0 && Total > 0 ? $"{Percent:F2}%" : GetFractionString();
        protected string GetRateString()
        {
            if(UsePercent || string.IsNullOrWhiteSpace(Suffix))
            {
                return "";
            }
            else
            {
                Update();
                return $"{FormatValue(Rate)}/s";
            }
        }
        protected string FormatValue(long value) => OrderScale == 0 || value == 0 ? $"{value}{Suffix}" : FormatValue((double)value);
        protected string FormatValue(double value) => $"{value.Format(3, OrderScale > 0 ? OrderScale : 1000, true, IsIntegral)}{Suffix}";
        protected virtual void UpdateFilter(double dt)
        {
            long dv = Value - lastFilterValue;
            double r = Math.Lerp(dv / dt, Rate, Math.Pow(filterConstant, dt));
            if(r > 1e10)
            {
                Console.WriteLine("");
            }
            Rate = Math.Lerp(dv / dt, Rate, Math.Pow(filterConstant, dt));
            lastFilterValue = Value;
        }
        protected void Update()
        {
            lock(this)
            {
                double elapsed = Elapsed;
                double dt = elapsed - lastFilterTime;
                if(dt > 0)
                {
                    lastFilterTime = elapsed;
                    UpdateFilter(dt);
                }
            }
        }

        private void OnUpdateTimerElapsed(object sender, System.Timers.ElapsedEventArgs e) => OnUpdate?.Invoke();
        private void HandleOnCompleted()
        {
            stopwatch.Stop();
            updateTimer.Stop();
        }
    }
}
