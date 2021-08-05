using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GoNetWasm.Data;
using GoNetWasm.Runtime;

namespace GoNetWasm.Internal
{
    internal static class Reflect
    {
        internal static void DeleteProperty(object obj, string name)
        {
            throw new NotImplementedException(nameof(DeleteProperty));
        }

        internal static object Get(object obj, object value)
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

        internal static object Apply(object obj, object instance, object[] args)
        {
            if (obj is Func<object, object[], object> func)
            {
                var result = func(instance, args);
                return result;
            }
            throw new NotImplementedException(nameof(Apply));
        }

        internal static object Construct(object obj, object[] args)
        {
            if (obj is Func<object> of)
            {
                var res = of();
                if (res is ICollection<byte> byteArray)
                {
                    for (var i = 0; i < (double) args[0]; i++)
                        byteArray.Add(0);
                    return res;
                }
                throw new NotImplementedException(res + " ?");
            }
            throw new NotImplementedException(nameof(Construct));
        }

        internal static void Set(object obj, object name, object rawValue)
        {
            if (name is string myName)
            {
                var type = obj.GetType();
                var prop = FindProperty(type, myName);
                var value = rawValue.IsUndefinedOrNull() ? null : rawValue;
                prop.SetValue(obj, value);
                return;
            }
            throw new NotImplementedException(nameof(Set));
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