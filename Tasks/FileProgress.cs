using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Utils
{
    public class FileProgress : Progress
    {
        public long BytesTotal { get; set; } = 0;
        public long Bytes
        {
            get => bytes;
            set
            {
                bytes = value;
            }
        }
        public double ByteRate { get; protected set; } = 0;

        protected long lastFilterBytes = 0;
        protected long bytes = 0;

        public FileProgress(string description, bool usePercent = true, long total = -1, long value = 0) : base(description, usePercent, total, value) { }
        public FileProgress(string description, bool usePercent, string suffix, long total = -1, long value = 0) : base(description, usePercent, suffix, total, value) { }
        public FileProgress(string description, double orderScale, bool isIntegral = true, long total = -1, long value = 0) : base(description, orderScale, isIntegral, total, value) { }
        public FileProgress(string description, double orderScale, string suffix, bool isIntegral = true, long total = -1, long value = 0) : base(description, orderScale, suffix, isIntegral, total, value) { }
        public override string ToString()
        {
            string buffer = UsePercent ? GetPercentString() : GetFractionString();
            buffer = buffer.AddIfNotNull(GetRateString(), " @");
            buffer += $"| {GetBytesString()}";
            buffer = buffer.AddIfNotNull(GetByteRateString(), " @");
            buffer = buffer.AddIfNotNull(GetTimeString(), ", ");
            buffer = Description.PadRight(30).AddIfNotNull(buffer, " (", ")");
            buffer = buffer.AddIfNotNull(CurrentItem, " ");
            return buffer;
        }

        protected string GetBytesString() => BytesTotal >= 0 ? (Bytes >= 0 ? $"{FormatBytes(Bytes)}/{FormatBytes(BytesTotal)}" : FormatBytes(BytesTotal)) : (Bytes >= 0 ? FormatBytes(Bytes) : "");
        protected string GetByteRateString()
        {
            Update();
            return $"{FormatBytes(ByteRate)}/s";
        }
        protected string FormatBytes(double value) => $"{value.Format(3, 1024.0, true, true)}B";
        protected override void UpdateFilter(double dt)
        {
            base.UpdateFilter(dt);

            long db = Bytes - lastFilterBytes;
            ByteRate = Math.Lerp(db / dt, ByteRate, Math.Pow(filterConstant, dt));
            lastFilterBytes = Bytes;
        }
    }
}
