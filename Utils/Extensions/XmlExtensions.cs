using System;
using System.Xml;

namespace Utils
{
    public static class XmlExtensions
    {
        public static T Get<T>(this XmlNode root, string key)
        {
            if (!root.TryGetInnerText(key, out string value) || !TryParse<T>(value, out T result))
            {
                throw new NullReferenceException($"<{root.Name}> doesn't contain child <{key}>.");
            }
            return result;
        }
        public static T Get<T>(this XmlNode root, string key, T defaultValue)
        {
            if (!root.TryGetInnerText(key, out string value) || !TryParse<T>(value, out T result))
            {
                return defaultValue;
            }
            return result;
        }
        public static XmlAttribute GetAttribute(this XmlNode node, string key)
        {
            foreach (object item in node.Attributes)
            {
                XmlAttribute attribute = item as XmlAttribute;
                if (attribute.Name.ToLower() == key.ToLower())
                {
                    return attribute;
                }
            }
            return null;
        }
        public static T GetAttribute<T>(this XmlNode root, string key)
        {
            XmlAttribute attribute = root.GetAttribute(key);
            string value = attribute?.Value ?? null;
            if (value == null || !TryParse<T>(value, out T result))
            {
                throw new NullReferenceException($"<{root.Name}> doesn't contain attribute '{key}'.");
            }
            return result;
        }
        public static T GetAttribute<T>(this XmlNode root, string key, T defaultValue)
        {
            XmlAttribute attribute = root.GetAttribute(key);
            string value = attribute?.Value ?? null;
            if (value == null || !TryParse<T>(value, out T result))
            {
                return defaultValue;
            }
            return result;
        }
        public static XmlNode GetNode(this XmlNode root, string key) => root.SelectSingleNode(key.ToLower());
        public static XmlNodeList GetNodes(this XmlNode root, string key) => root.SelectNodes(key.ToLower());
        public static bool TryParse<T>(string value, out T result)
        {
            try
            {
                if (typeof(T) == typeof(bool))
                {
                    result = (T)(object)ParseBool(value);
                    return true;
                }
                else if (typeof(T) == typeof(Guid))
                {
                    result = (T)(object)Guid.Parse(value);
                    return true;
                }
                else
                {
                    result = (T)Convert.ChangeType(value, typeof(T));
                    return true;
                }
            }
            catch
            {
                result = default;
                return false;
            }
        }
        
        private static bool ParseBool(string value)
        {
            switch (value.ToLower())
            {
                case "true":
                case "t":
                case "1":
                    return true;
                case "false":
                case "f":
                case "0":
                    return false;
                default:
                    throw new FormatException($"Cannot parse '{value}' as bool.");
            }
        }
        private static bool TryGetInnerText(this XmlNode root, string key, out string value)
        {
            XmlNode node = GetNode(root, key);
            if (node != null)
            {
                value = node.InnerText;
                return true;
            }
            value = default;
            return false;
        }
    }
}
