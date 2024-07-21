using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Utils
{
    public static class ConsoleExtensions
    {
        public class SelectionItem<T>
        {
            public string name;
            public Regex pattern;
            public T value;

            public SelectionItem(string name, Regex pattern, T value)
            {
                this.name = name;
                this.pattern = pattern;
                this.value = value;
            }
        }

        private static readonly Regex numberRegex = new Regex(@"^\s*(\d+)\s*$");

        public static bool TrySelectInline(string prompt, IEnumerable<string> items, out string value, string errorPrompt = null) => TrySelectInline(prompt, items, int.MaxValue, out value, errorPrompt);
        public static bool TrySelectInline(string prompt, IEnumerable<string> items, int maxTries, out string value, string errorPrompt = null) => TrySelectInline(prompt, items.Select(item => new SelectionItem<string>(item, new Regex(item), item)), maxTries, out value, errorPrompt);
        public static bool TrySelectInline<T>(string prompt, IEnumerable<(string, T)> items, out T value, string errorPrompt = null) => TrySelectInline(prompt, items, int.MaxValue, out value, errorPrompt);
        public static bool TrySelectInline<T>(string prompt, IEnumerable<(string, T)> items, int maxTries, out T value, string errorPrompt = null) => TrySelectInline<T>(prompt, items.Select(item => new SelectionItem<T>(item.Item1, new Regex(item.Item1), item.Item2)), maxTries, out value, errorPrompt);
        public static bool TrySelectInline<T>(string prompt, IEnumerable<KeyValuePair<string, T>> items, out T value, string errorPrompt = null) => TrySelectInline(prompt, items, int.MaxValue, out value, errorPrompt);
        public static bool TrySelectInline<T>(string prompt, IEnumerable<KeyValuePair<string, T>> items, int maxTries, out T value, string errorPrompt = null) => TrySelectInline(prompt, items.Select(item => new SelectionItem<T>(item.Key, new Regex(item.Key), item.Value)), int.MaxValue, out value, errorPrompt);
        public static bool TrySelectInline<T>(string prompt, IEnumerable<SelectionItem<T>> items, out T value, string errorPrompt = null) => TrySelectInline(prompt, items, int.MaxValue, out value, errorPrompt);
        public static bool TrySelectInline<T>(string prompt, IEnumerable<SelectionItem<T>> items, int maxTries, out T value, string errorPrompt = null)
        {
            prompt = prompt.Trim() + $" ({string.Join("/", items.Select(item => item.name))}): ";
            if(errorPrompt == null)
            {
                errorPrompt = $"Unknown selection: {prompt}";
            }

            value = default;
            for(int i = 0; i < maxTries; i++)
            {
                Console.Write(prompt);
                string response = Console.ReadLine();
                if(string.IsNullOrEmpty(response))
                {
                    return false;
                }
                foreach(SelectionItem<T> item in items)
                {
                    if(item.pattern.IsMatch(response))
                    {
                        value = item.value;
                        return true;
                    }
                }
                prompt = errorPrompt;
            }
            return false;
        }
        public static bool TrySelectList(string introduction, string prompt, IEnumerable<string> items, out string value, string errorPrompt = null) => TrySelectList(introduction, prompt, items, int.MaxValue, out value, errorPrompt);
        public static bool TrySelectList(string introduction, string prompt, IEnumerable<string> items, int maxTries, out string value, string errorPrompt = null) => TrySelectList(introduction, prompt, items.Select(item => new SelectionItem<string>(item, new Regex(item), item)), int.MaxValue, out value, errorPrompt);
        public static bool TrySelectList<T>(string introduction, string prompt, IEnumerable<(string, T)> items, out T value, string errorPrompt = null) => TrySelectList(introduction, prompt, items, int.MaxValue, out value, errorPrompt);
        public static bool TrySelectList<T>(string introduction, string prompt, IEnumerable<(string, T)> items, int maxTries, out T value, string errorPrompt = null) => TrySelectList(introduction, prompt, items.Select(item => new SelectionItem<T>(item.Item1, new Regex(item.Item1), item.Item2)), maxTries, out value, errorPrompt);
        public static bool TrySelectList<T>(string introduction, string prompt, IEnumerable<KeyValuePair<string, T>> items, out T value, string errorPrompt = null) => TrySelectList(introduction, prompt, items, int.MaxValue, out value, errorPrompt);
        public static bool TrySelectList<T>(string introduction, string prompt, IEnumerable<KeyValuePair<string, T>> items, int maxTries, out T value, string errorPrompt = null) => TrySelectList(introduction, prompt, items.Select(item => new SelectionItem<T>(item.Key, new Regex(item.Key), item.Value)), int.MaxValue, out value, errorPrompt);
        public static bool TrySelectList<T>(string introduction, string prompt, IEnumerable<SelectionItem<T>> items, out T value, string errorPrompt = null) => TrySelectList(introduction, prompt, items, int.MaxValue, out value, errorPrompt);
        public static bool TrySelectList<T>(string introduction, string prompt, IEnumerable<SelectionItem<T>> items, int maxTries, out T value, string errorPrompt = null)
        {
            if(string.IsNullOrWhiteSpace(introduction))
            {
                introduction = "Select from the following:";
            }
            int number = 0;
            foreach(SelectionItem<T> item in items)
            {
                ++number;
                introduction += $"\n {number,3} {item.name}";
            }
            if(string.IsNullOrWhiteSpace(prompt))
            {
                prompt = "";
            }
            prompt += ": ";
            if(errorPrompt == null)
            {
                errorPrompt = $"Unknown selection: {prompt}";
            }

            value = default;
            Console.WriteLine(introduction);
            for(int i = 0; i < maxTries; i++)
            {
                Console.Write(prompt);
                string response = Console.ReadLine();
                if(string.IsNullOrEmpty(response))
                {
                    return false;
                }
                if(numberRegex.TryMatch(response, out response))
                {
                    number = int.Parse(response);
                    if(number > 0 && number <= items.Count())
                    {
                        SelectionItem<T> item = items.ElementAt(number - 1);
                        value = item.value;
                        return true;
                    }
                }
                foreach(SelectionItem<T> item in items)
                {
                    if(item.pattern.IsMatch(response))
                    {
                        value = item.value;
                        return true;
                    }
                }
                prompt = errorPrompt;
            }
            return false;
        }
    }
}
