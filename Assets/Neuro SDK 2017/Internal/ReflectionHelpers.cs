
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NeuroSdk.Internal
{
    internal static class ReflectionHelpers
    {
        public static IEnumerable<T> GetAllInAssembly<T>(Assembly assembly, Transform parent)
        {
            IEnumerable<Type> types = assembly.GetTypes()
                .Where(type => !type.IsAbstract)
                .Where(type => typeof(T).IsAssignableFrom(type));

            foreach (Type type in types)
            {
                MethodInfo method = type.GetMethod("CreateInstance", BindingFlags.Static | BindingFlags.Public);

                if (method != null)
                {
                    yield return (T) method.Invoke(null, null);
                }
                else if (typeof(Component).IsAssignableFrom(type))
                {
                    GameObject obj = new GameObject(type.FullName);
                    obj.transform.SetParent(parent);
                    yield return (T) (object) obj.AddComponent(type);
                }
                else
                {
                    yield return (T) Activator.CreateInstance(type);
                }
            }
        }
    }
}
