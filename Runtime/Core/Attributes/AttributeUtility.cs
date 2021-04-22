using System;
using System.Linq;
using System.Reflection;

namespace Unity.LiveCapture
{
    /// <summary>
    /// Contains utility methods for handling attributes.
    /// </summary>
    static class AttributeUtility
    {
        /// <summary>
        /// Gets an attribute instance decorating a member.
        /// </summary>
        /// <param name="member">The member on which to search for the attribute.</param>
        /// <param name="inherit">When true, inspects the ancestors of the <paramref name="member"/> for the attribute.</param>
        /// <typeparam name="T">The type of attribute to search for.</typeparam>
        /// <returns>The first matching attribute instance, or null if the attribute was not found on the type.</returns>
        public static T GetAttribute<T>(this MemberInfo member, bool inherit = false) where T : Attribute
        {
            return member.GetAttributes<T>().FirstOrDefault();
        }

        /// <summary>
        /// Gets all of the attribute instances decorating a member.
        /// </summary>
        /// <param name="member">The member on which to search for the attribute.</param>
        /// <param name="inherit">When true, inspects the ancestors of the <paramref name="member"/> for the attribute.</param>
        /// <typeparam name="T">The type of attribute to search for.</typeparam>
        /// <returns>A new array containing the attribute instances, or an empty array if the attribute was not found on the type.</returns>
        public static T[] GetAttributes<T>(this MemberInfo member, bool inherit = false) where T : Attribute
        {
            return member.GetCustomAttributes(typeof(T), inherit).Cast<T>().ToArray();
        }

        /// <summary>
        /// Searches all assemblies for all types decorated with a given attribute.
        /// </summary>
        /// <param name="inherit">When true, inspects the ancestors of each type for the attribute.</param>
        /// <typeparam name="T">The type of attribute to search for.</typeparam>
        /// <returns>A new array of tuples containing types using the attribute and the attribute instances.</returns>
        public static (Type type, T[] attributes)[] GetAllTypes<T>(bool inherit = false) where T : Attribute
        {
            return AppDomain.CurrentDomain.GetAssemblies().AsParallel()
                .SelectMany(assembly => assembly.GetTypes())
                .Select(type => (type, type.GetAttributes<T>(inherit)))
                .Where(tuple => tuple.Item2.Length > 0)
                .ToArray();
        }

        /// <summary>
        /// Searches all assemblies for all methods decorated with a given attribute.
        /// </summary>
        /// <param name="methodFlags">The flags controlling the method search.</param>
        /// <param name="inherit">When true, inspects the ancestors of each type for the attribute.</param>
        /// <typeparam name="T">The type of attribute to search for.</typeparam>
        /// <returns>A new array of tuples containing methods using the attribute and the attribute instances.</returns>
        public static (MethodInfo method, T[] attributes)[] GetAllMethods<T>(BindingFlags methodFlags, bool inherit = false) where T : Attribute
        {
            return AppDomain.CurrentDomain.GetAssemblies().AsParallel()
                .SelectMany(assembly => assembly.GetTypes())
                .SelectMany(type =>  type.GetMethods(methodFlags))
                .Select(method => (method, method.GetAttributes<T>(inherit)))
                .Where(tuple => tuple.Item2.Length > 0)
                .ToArray();
        }
    }
}
