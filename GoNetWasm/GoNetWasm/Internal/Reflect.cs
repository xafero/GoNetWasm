using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GoNetWasm.Runtime;

namespace GoNetWasm.Internal
{
    internal static class Reflect
    {
        public static void DeleteProperty(object obj, string name)
        {
            throw new NotImplementedException(nameof(DeleteProperty));
        }

        public static object Get(object obj, object value)
        {
            if (obj is Globals g && value is string key)
                return g[key];

            if (value is string name)
            {
                var type = obj.GetType();

                var property = FindProperty(type, name);
                if (property != null)
                    return property.GetValue(obj);

                var method = FindMethod(type, name);
                if (method != null)
                    return Wrap(method);

                var field = FindField(type, name);
                if (field != null)
                    return field.GetValue(obj);

                throw new InvalidOperationException($"{type} {name}");
            }

            if (obj is ICollection<object> coll)
            {
                var index = (long) value;
                var item = coll.ElementAt((int) index);
                return item;
            }

            throw new NotImplementedException(nameof(Get));
        }

        private static object Wrap(MethodBase method)
        {
            object Func(object obj, object[] args)
            {
                return method.Invoke(obj, args);
            }

            return (Func<object, object[], object>) Func;
        }

        private static FieldInfo FindField(Type type, string name)
            => type.GetFields().FirstOrDefault(f =>
                f.Name.Equals(name.TrimStart('_'), StringComparison.InvariantCultureIgnoreCase));

        private static MethodInfo FindMethod(Type type, string name)
            => type.GetMethods().FirstOrDefault(m =>
                m.Name.Equals(name.TrimStart('_'), StringComparison.InvariantCultureIgnoreCase));

        private static PropertyInfo FindProperty(Type type, string name)
            => type.GetProperties().FirstOrDefault(p =>
                p.Name.Equals(name.TrimStart('_'), StringComparison.InvariantCultureIgnoreCase));
    }
}