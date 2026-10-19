using DiGi.GML.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DiGi.GML.Classes
{
    /// <summary>
    /// Provides functionality to manage and retrieve types that implement the <see cref="IAbstractGML"/> interface.
    /// </summary>
    public class TypeManager
    {
        private Dictionary<string, Type?> dictionary = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeManager"/> class.
        /// </summary>
        public TypeManager()
        {
        }

        /// <summary>
        /// Retrieves a type based on the provided name from all loaded assemblies, ensuring it implements <see cref="IAbstractGML"/>.
        /// <para>An assembly whose types cannot all be loaded, for example because one of its dependencies is missing from the host, does not fail the lookup. Its loadable types are searched, and the rest are skipped.</para>
        /// </summary>
        /// <param name="name">The name of the type to search for.</param>
        /// <param name="update">If set to <c>true</c>, the result (including null) will be cached in the internal dictionary to optimize future lookups.</param>
        /// <returns>The <see cref="Type"/> if found and it implements <see cref="IAbstractGML"/>; otherwise, <c>null</c>.</returns>
        public Type? GetType(string? name, bool update = true)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            dictionary ??= [];

            if (dictionary.TryGetValue(name!, out Type? type))
            {
                return type;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                // An assembly with a dependency missing from the host (e.g. System.Private.Windows.Core, which
                // references System.Formats.Nrbf) throws from GetTypes; its loadable types are still searched
                Type?[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException reflectionTypeLoadException)
                {
                    types = reflectionTypeLoadException.Types;
                }

                foreach (Type? type_Temp in types)
                {
                    if (type_Temp == null || !typeof(IAbstractGML).IsAssignableFrom(type_Temp))
                    {
                        continue;
                    }

                    if (type_Temp.Name != name)
                    {
                        continue;
                    }

                    if (update)
                    {
                        dictionary[name] = type_Temp;
                    }

                    return type_Temp;
                }
            }

            if (update)
            {
                dictionary[name!] = null;
            }

            return null;
        }
    }
}
