using System;
using System.Collections.Generic;
using System.Linq;
using Utils;

namespace Utils.Queries
{
    public class MultipleChoiceOption<T> : Option
    {
        public OrderedDictionary<string, T> Choices = new OrderedDictionary<string, T>();
        public string Key
        {
            get => key;
            set
            {
                if(Choices.ContainsKey(value))
                {
                    key = value;
                    OnChanged();
                }
                else
                {
                    throw new ArgumentException($"Key does not exist in map: '{value}'");
                }
            }
        }
        public T Value => Choices[Key];

        private string key = null;

        public MultipleChoiceOption(string name, IEnumerable<KeyValuePair<string, T>> choices) : base(name)
        {
            foreach(KeyValuePair<string, T> item in choices)
            {
                Choices.Add(item);
            }
        }
        public MultipleChoiceOption(string name, IEnumerable<T> choices) : this(name, choices.Select(choice => new KeyValuePair<string, T>(choice.ToString(), choice))) { }
        public string[] GetChoiceNames() => Choices.Keys.ToArray();
    }
}
