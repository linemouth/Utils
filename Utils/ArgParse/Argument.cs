using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Utils.ArgParse
{
    public class Argument
    {
        public readonly string Name;
        public readonly string Description;
        public string[] Patterns { get; protected set; }
        public readonly string Group;
        public readonly ArgumentMode Mode;
        public readonly IConvertible DefaultValue = null;
        public readonly IConvertible ConstValue = null;
        public readonly Type Type;
        public readonly Type SingleType;
        public bool IsPositional => Patterns == null;
        public bool IsAppend => Mode == ArgumentMode.Append || Mode == ArgumentMode.AppendConst;
        public string[] PatternsOrName => Patterns ?? new[] { Name };
        public string Usage
        {
            get
            {
                List<string> patternLines = string.Join(", ", PatternsOrName).WrapLines(usageFlagWidth);
                int descriptionLength = usageTotalWidth - 5 - usageFlagWidth;
                List<string> descriptionLines = Description.WrapLines(descriptionLength);
                string buffer = "";
                for(int i = 0; i < 10 && (i < patternLines.Count || i < descriptionLines.Count); ++i)
                {
                    if(i > 0)
                    {
                        buffer += "\n";
                    }
                    buffer += $"│   {(i < patternLines.Count ? patternLines[i] : "").PadRight(usageFlagWidth)}   {(i < descriptionLines.Count ? descriptionLines[i] : "").PadRight(descriptionLength)} │";
                }
                return buffer;
            }
        }

        internal const int usageTotalWidth = 96;
        internal const int usageFlagWidth = 16;

        public Argument(ArgumentMode mode, string name, string description, IConvertible defaultValue = null, IConvertible constValue = null, string group = null) : this(mode, name, description, (string[])null, defaultValue, constValue, group) { }
        public Argument(ArgumentMode mode, string name, string description, string pattern, IConvertible defaultValue = null, IConvertible constValue = null, string group = null) : this(mode, name, description, pattern == null ? null : new[] { pattern }, defaultValue, constValue, group) { }
        public Argument(ArgumentMode mode, string name, string description, string[] patterns, IConvertible defaultValue = null, IConvertible constValue = null, string group = null)
        {
            Name = name;
            Description = description;
            Group = group;
            Mode = mode;
            Patterns = patterns;
            DefaultValue = defaultValue;
            ConstValue = constValue;

            // Check mode compatibility
            switch(mode)
            {
                case ArgumentMode.Store:
                case ArgumentMode.Append:
                    // No special restrictions
                    break;
                case ArgumentMode.StoreConst:
                case ArgumentMode.AppendConst:
                case ArgumentMode.Count:
                    if(IsPositional)
                    {
                        // There's no value to grab from the argument list, so we only use prefixes with these Arguments.
                        throw new ArgumentException("StoreConst, AppendConst, Count, and Print Arguments must use prefix flags.");
                    }
                    break;
            }
        }
    }
}
