using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Utils.ArgParse
{
    public class ArgumentParser
    {
        #region Public Properties
        public delegate IConvertible Getter(string prompt);
        public delegate List<IConvertible> ListGetter(string prompt);
        public string Title { get; set; }
        public string Description { get; set; }
        public string Usage
        {
            get
            {
                // Get the command path, but skip the last one if it's the --help argument.
                Argument[] commands = (CommandPath.LastOrDefault() == usageCommand ? CommandPath.Reverse().Skip(1).Reverse() : CommandPath).ToArray();

                string title = Title;
                string description = commands.LastOrDefault()?.Description ?? Description;
                if(commands.Length > 0)
                {
                    title += $": {string.Join("/", CommandPath.Select(c => c.Name))}";
                    title = string.Join(" » ", new string[] { Title }.Concat(commands.Select(a => a.Name)));
                }
                string buffer = $"  {title}  ";
                buffer = $"{new string('─', (Argument.usageTotalWidth - buffer.Length) / 2)}{buffer}";
                buffer = $"┌─{buffer.PadRight(Argument.usageTotalWidth, '─')}─┐";
                string group = null;
                buffer += $"{string.Join("", description.WrapLines(Argument.usageTotalWidth).Select(line => $"\n│ {line.PadRight(Argument.usageTotalWidth)} │"))}";

                foreach(Argument argument in Arguments)
                {
                    if(argument.Group != group)
                    {
                        group = argument.Group;
                        buffer += $"\n│ {new string(' ', Argument.usageTotalWidth)} │";
                        if(!string.IsNullOrWhiteSpace(group))
                        {
                            buffer += $"\n│ {group.PadRight(Argument.usageTotalWidth)} │";
                        }
                    }
                    buffer += $"\n{argument.Usage}";
                }
                buffer += $"\n└─{new string('─', Argument.usageTotalWidth)}─┘";
                return buffer;
            }
        }
        public Argument Command => CommandPath.LastOrDefault();
        public Argument[] CommandPath => commandPath.ToArray();
        public Getter InteractiveGetter { get; set; }
        public ListGetter InteractiveListGetter { get; set; }
        #endregion

        #region Private Fields
        private IEnumerable<Argument> Arguments => Commands.Concat(arguments);
        private IEnumerable<Argument> Commands => new[] { usageCommand }.Concat(commands);
        private readonly Dictionary<string, List<IConvertible>> values = new Dictionary<string, List<IConvertible>>();
        private readonly List<Argument> arguments = new List<Argument>();
        private readonly List<Argument> commands = new List<Argument>();
        private readonly List<KeyValuePair<string, List<Argument>>> argumentGroups = new List<KeyValuePair<string, List<Argument>>> { new KeyValuePair<string, List<Argument>>("General", new List<Argument>()) };
        private readonly List<Argument> commandPath = new List<Argument>();
        private static readonly Argument usageCommand = new Argument(ArgumentMode.Command, "Usage", "Show this help dialog.", "--help", null, null, "Commands");
        private string currentGroup = null;
        #endregion

        #region Public Methods
        public ArgumentParser(string title, string description = "", Getter getter = null, ListGetter listGetter = null)
        {
            Title = title;
            Description = description;
            InteractiveGetter = getter ?? DefaultInteractiveGetter;
            InteractiveListGetter = listGetter ?? DefaultInteractiveListGetter;
        }
        public void Reset()
        {
            values.Clear();
            foreach(Argument argument in arguments)
            {
                if(!values.ContainsKey(argument.Description))
                {
                    switch(argument.Mode)
                    {
                        case ArgumentMode.Store:
                        case ArgumentMode.StoreConst:
                            values.Add(argument.Name, new List<IConvertible> { argument.DefaultValue });
                            break;
                        case ArgumentMode.Count:
                            values.Add(argument.Name, new List<IConvertible> { 0 });
                            break;
                        case ArgumentMode.Append:
                        case ArgumentMode.AppendConst:
                            values.Add(argument.Name, new List<IConvertible>());
                            break;
                    }
                }
            }
        }
        public void ClearArguments()
        {
            commands.Clear();
            arguments.Clear();
            currentGroup = null;
        }
        public string ParseArguments(ref string[] args)
        {
            // Clear values back to their defaults
            Reset();

            // Check for a command. If present, return immediately.
            if(args.Length == 0)
            {
                throw new ArgumentException("Not enough arguments provided");
            }
            string arg = args[0];
            Argument argument = Commands.FirstOrDefault(a => a.PatternsOrName.Contains(arg));
            if(argument != null)
            {
                commandPath.Add(argument);
                args = args.Skip(1).ToArray();
            }
            else
            {
                // Sort flags and positional arguments
                IEnumerator<Argument> positionals = arguments.Where(a => a.IsPositional).GetEnumerator();
                IEnumerable<Argument> flags = arguments.Where(a => !a.IsPositional);

                // Parse the remaining arguments
                for(int index = 0; index < args.Length; ++index)
                {
                    arg = args[index];

                    // Look for a new flag prefix
                    Argument flag = flags.FirstOrDefault(f =>
                    {
                        return f.Patterns.Any(p =>
                        {
                            return p == arg;
                        });
                    });
                    if(flag != null)
                    {
                        // Replace the previous argument.
                        argument = flag;

                        // If the flag does not take values, process it now.
                        switch(flag.Mode)
                        {
                            case ArgumentMode.Store: // Store Arguments overwrite a value with the following argument.
                                ++index;
                                if(index >= args.Length)
                                {
                                    throw new ArgumentException("Unexpected end of argument list.");
                                }
                                Set(flag.Name, args[index]);
                                continue;
                            case ArgumentMode.Append: // Append Arguments add following value(s) to a list.
                                ++index;
                                if(index >= args.Length)
                                {
                                    throw new ArgumentException("Unexpected end of argument list.");
                                }
                                Append(argument.Name, args[index]);
                                argument = flag; // Save the argument so it can store more than one value at a time.
                                continue;
                            case ArgumentMode.StoreConst: // StoreConst Arguments overwrite a value with their ConstValue.
                                Set(flag.Name, flag.ConstValue);
                                continue;
                            case ArgumentMode.AppendConst: // AppendConst Arguments append their ConstValue to a list.
                                                           // Because the type isn't known until runtime, a bit more work is needed to keep things generic.
                                Append(flag.Name, flag.ConstValue);
                                continue;
                            case ArgumentMode.Count: // Count Arguments look only for prefixes and increment an integer by their ConstValue.
                                values[flag.Name][0] = (int)values[flag.Name][0] + 1;
                                continue;
                        }
                    }

                    // If no new flag was detected, then the current arg could be a new positional argument or an item in an ongoing Append argument.
                    if(argument?.Mode == ArgumentMode.Append)
                    {
                        Append(argument.Name, arg);
                        continue;
                    }

                    // If we got this far, then we must assume that arg is a positional argument.
                    if(positionals.MoveNext())
                    {
                        switch(positionals.Current.Mode)
                        {
                            case ArgumentMode.Store: // Store Arguments overwrite a value with the following argument.
                                Set(positionals.Current.Name, arg);
                                continue;
                            case ArgumentMode.Append: // Append Arguments add following value(s) to a list.
                                Append(positionals.Current.Name, arg);
                                argument = positionals.Current; // Save the argument so it can store more than one value at a time.
                                continue;
                            default:
                                throw new InvalidOperationException("Positional arguments can only be Store or Append.");
                        }
                    }

                    // If we get here, then we got into some invalid state. Suspect that incompatible junk was fed to the function.
                    throw new ArgumentException($"Invalid argument at position {index}.");
                }
            }

            return Command?.Name;
        }
        public void AddGroup(string title) => currentGroup = title;
        public void AddArgument(Argument argument)
        {
            if(argument.Mode == ArgumentMode.Command)
            {
                commands.Add(argument);
            }
            else
            {
                arguments.Add(argument);
            }
        }
        public void AddCommandArgument(string name, string description, string prefix = null) => AddArgument(new Argument(ArgumentMode.Command, name, description, prefix, null, null, "Commands"));
        public void AddStoreArgument(string name, string description, string prefix = null, IConvertible defaultValue = null) => AddArgument(new Argument(ArgumentMode.Store, name, description, prefix, defaultValue, null, currentGroup));
        public void AddStoreArgument(string name, string description, string[] prefixes, IConvertible defaultValue = null) => AddArgument(new Argument(ArgumentMode.Store, name, description, prefixes, defaultValue, null, currentGroup));
        public void AddStoreConstArgument(string name, string description, string prefix, IConvertible defaultValue, IConvertible constValue) => AddArgument(new Argument(ArgumentMode.StoreConst, name, description, prefix, defaultValue, constValue, currentGroup));
        public void AddStoreConstArgument(string name, string description, string[] prefixes, IConvertible defaultValue, IConvertible constValue) => AddArgument(new Argument(ArgumentMode.StoreConst, name, description, prefixes, defaultValue, constValue, currentGroup));
        public void AddAppendArgument(string name, string description, string prefix = null, IConvertible defaultValue = null) => AddArgument(new Argument(ArgumentMode.Append, name, description, prefix, defaultValue, null, currentGroup));
        public void AddAppendArgument(string name, string description, string[] prefixes, IConvertible defaultValue = null) => AddArgument(new Argument(ArgumentMode.Append, name, description, prefixes, defaultValue, null, currentGroup));
        public void AddAppendConstArgument(string name, string description, string prefix, IConvertible defaultValue = null) => AddArgument(new Argument(ArgumentMode.AppendConst, name, description, prefix, defaultValue, null, currentGroup));
        public void AddAppendConstArgument(string name, string description, string[] prefixes, IConvertible defaultValue = null) => AddArgument(new Argument(ArgumentMode.AppendConst, name, description, prefixes, defaultValue, null, currentGroup));
        public void AddCountArgument(string name, string description, string prefix, IConvertible defaultValue = null) => AddArgument(new Argument(ArgumentMode.Count, name, description, prefix, defaultValue, null, currentGroup));
        public void AddCountArgument(string name, string description, string[] prefixes, IConvertible defaultValue = null) => AddArgument(new Argument(ArgumentMode.Count, name, description, prefixes, defaultValue, null, currentGroup));
        public T Get<T>(string key, string prompt = null) where T : IConvertible
        {
            InteractiveGetIfNull(key, prompt);
            return Conversion.Convert<T>(values[key][0]);
        }
        public List<T> GetList<T>(string key, string prompt = null) where T : IConvertible
        {
            InteractiveGetListIfEmpty(key, prompt);
            return Conversion.ConvertAll<T>(values[key]).ToList();
        }
        public bool TryGet<T>(string key, out T value, string prompt = null) where T : IConvertible
        {
            try
            {
                value = Get<T>(key, prompt);
                return value != null;
            }
            catch { }
            value = default;
            return false;
        }
        public bool TryGetList<T>(string key, out List<T> value, string prompt = null) where T : IConvertible
        {
            try
            {
                value = GetList<T>(key, prompt);
                return value != null;
            }
            catch { }
            value = null;
            return false;
        }
        public T GetOrDefalt<T>(string key, T defaultValue) where T : IConvertible => TryGet(key, out T value) ? value : defaultValue;
        /// <summary>Returns a list of values named by the given key. If the list is empty or does not exist</summary>
        public List<T> GetListOrDefalt<T>(string key, List<T> defaultValue) where T : IConvertible => TryGetList(key, out List<T> value) ? value : defaultValue;
        #endregion

        #region Interactive Prompts
        /// <summary>Prints a prompt to the console, then reads the user's input and converts the value to the chosen type.</summary>
        public static T Prompt<T>(string prompt) where T : IConvertible
        {
            Console.WriteLine(prompt);
            return Conversion.Convert<T>(prompt);
        }
        /// <summary>Prints a prompt to the console, then reads the user's input and checks for a yes/no type response. The prompt is repeated as long as the user does not choose a valid input.</summary>
        /// <param name="truePattern">A pattern against which to match a valid "yes" response.</param>
        /// <param name="falsePattern">A pattern against which to match a valid "no" response.</param>
        public static bool PromptBool(string prompt, string truePattern = "^y(es)?$", string falsePattern = "^n(o)?$")
        {
            while(true)
            {
                Console.WriteLine(prompt);
                string input = Console.ReadLine();
                if(new Regex(truePattern, RegexOptions.IgnoreCase).Match(input).Success)
                {
                    return true;
                }
                if(new Regex(falsePattern, RegexOptions.IgnoreCase).Match(input).Success)
                {
                    return false;
                }
            }
        }
        #endregion

        #region Helper Functions
        /// <summary>Sets the given named value to hold a single value.</summary>
        private void Set(string destination, IConvertible value) => values[destination][0] = value;
        /// <summary>Appends a value to the given named value.</summary>
        private void Append(string destination, IConvertible value) => values[destination].Add(value);
        /// <summary>Checks if the given named value is null. If so, the user is prompted for a value.</summary>
        private void InteractiveGetIfNull(string key, string prompt)
        {
            if(values[key][0] == null && prompt != null)
            {
                values[key][0] = InteractiveGetter(prompt);
            }
        }
        /// <summary>Checks if the given named value is an empty list. If so, the user is prompted for a value.</summary>
        private void InteractiveGetListIfEmpty(string key, string prompt)
        {
            if(values[key].Count == 0 && prompt != null)
            {
                values[key] = InteractiveListGetter(prompt);
            }
        }
        /// <summary>The default getter for single values.</summary>
        private IConvertible DefaultInteractiveGetter(string prompt)
        {
            Console.WriteLine(prompt);
            return Console.ReadLine();
        }
        /// <summary>The default getter for list values.</summary>
        private List<IConvertible> DefaultInteractiveListGetter(string prompt)
        {
            List<IConvertible> results = new List<IConvertible>();
            Console.WriteLine(prompt);
            while(true)
            {
                string input = Console.ReadLine();
                if(string.IsNullOrWhiteSpace(input))
                {
                    break;
                }
                results.Add(input);
            }
            return results;
        }
        #endregion
    }
}
