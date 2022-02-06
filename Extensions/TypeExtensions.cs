using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Runtime;

namespace Utils
{
    public static class TypeExtensions
    {
        public static readonly Map<string, Type> TypeAliases = new Map<string, Type>
        {
            { "bool",     typeof(System.Boolean ) }, // IConvertible
            { "sbyte",    typeof(System.SByte   ) }, // IConvertible
            { "byte",     typeof(System.Byte    ) }, // IConvertible
            { "char",     typeof(System.Char    ) }, // IConvertible
            { "short",    typeof(System.Int16   ) }, // IConvertible
            { "ushort",   typeof(System.UInt16  ) }, // IConvertible
            { "int",      typeof(System.Int32   ) }, // IConvertible
            { "uint",     typeof(System.UInt32  ) }, // IConvertible
            { "long",     typeof(System.Int64   ) }, // IConvertible
            { "ulong",    typeof(System.UInt64  ) }, // IConvertible
            { "decimal",  typeof(System.Decimal ) }, // IConvertible
            { "float",    typeof(System.Single  ) }, // IConvertible
            { "double",   typeof(System.Double  ) }, // IConvertible
            { "dateTime", typeof(System.DateTime) }, // IConvertible
            { "enum",     typeof(System.Enum    ) }, // IConvertible
            { "string",   typeof(System.String  ) }, // IConvertible
            { "object",   typeof(System.Object  ) }
        };
        public static Type GetTypeByFullName(string qualifiedName, bool caseInsensitive = false)
        {
            Match match = new Regex(@"^(.*)\.(\w+)$", caseInsensitive ? RegexOptions.IgnoreCase : RegexOptions.None).Match(qualifiedName);
            if(match.Success)
            {
                string assemblyName = match.Groups[1].Value;
                string typeName = match.Groups[2].Value;
                foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if(assembly.GetName().Name == assemblyName)
                    {
                        foreach(Type type in assembly.GetTypes())
                        {
                            if(type.Name == typeName)
                            {
                                return type;
                            }
                        }
                    }
                }
            }
            else if(TypeAliases.Forward.TryGetValue(qualifiedName, out Type type))
            {
                return type;
            }

            return null;
        }
        public static Type[] GetAllTypes() => (from assembly in AppDomain.CurrentDomain.GetAssemblies() from type in assembly.GetTypes() select type).ToArray();
        public static Type[] GetTypesInNamespace(string namespaceName) => GetAllTypes().Where(t => t.Namespace == namespaceName).ToArray();
        public static Type[] GetSubclassesOf(this Type type)
        {
            IEnumerable<Type> results = null;

            if(type.IsInterface)
            {
                results = GetAllTypes().Where(t => type.IsAssignableFrom(t) || t.GetInterfaces().Where(i => i.IsGenericType).Any(i => i.GetGenericTypeDefinition() == type));
            }
            else
            {
                results = GetAllTypes().Where(t => t.IsSubclassOf(type));
            }

            return results.Except(new Type[] { type }).ToArray();
        }
    }
}
