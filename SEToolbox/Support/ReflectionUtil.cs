using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using System.Runtime.CompilerServices;
using System.Windows.Navigation;


namespace SEToolbox.Support
{
    public static class ReflectionUtil
    {
        /// <summary>
        /// Replaces the method.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="dest">The dest.</param>
        public static void ReplaceMethod(MethodBase source, MethodBase dest)
        {
            if (!MethodSignaturesEqual(source, dest))
            {
                throw new ArgumentException("The method signatures are not the same.", nameof(source));
            }

            ReplaceMethod(GetMethodAddress(source), dest);
        }

        /// <summary>
        /// Replaces the method.
        /// </summary>
        /// <param name="srcAdr">The SRC adr.</param>
        /// <param name="dest">The dest.</param>
        public unsafe static void ReplaceMethod(IntPtr srcAdr, MethodBase dest)
        {
            IntPtr destAdr = GetMethodAddress(dest);

            if (IntPtr.Size == 8)
            {
                ulong* d = (ulong*)destAdr.ToPointer();
                *d = *(ulong*)srcAdr.ToPointer();
            }
            else
            {
                uint* d = (uint*)destAdr.ToPointer();
                *d = *(uint*)srcAdr.ToPointer();
            }
        }

        /// <summary>
        /// Gets the address of the method stub
        /// </summary>
        /// <param name="method">The method handle.</param>
        /// <returns></returns>
        public static IntPtr GetMethodAddress(MethodBase method)
        {
            if (method is DynamicMethod)
            {
                return GetDynamicMethodAddress(method);
            }

            // Prepare the method so it gets jited
            RuntimeHelpers.PrepareMethod(method.MethodHandle);

            unsafe 
            { 
                return new IntPtr((int*)method.MethodHandle.Value.ToPointer() + 2);
            }
        }

        private unsafe static IntPtr GetDynamicMethodAddress(MethodBase method)
        {
            RuntimeMethodHandle handle = GetDynamicMethodRuntimeHandle(method);
            byte* ptr = (byte*)handle.Value.ToPointer();

            if (IntPtr.Size == 8)
            {
                ulong* address = (ulong*)ptr;
                address += 6;
                return new IntPtr(address);
            }
            else
            {
                uint* address = (uint*)ptr;
                address += 6;
                return new IntPtr(address);
            }
        }

        private static RuntimeMethodHandle GetDynamicMethodRuntimeHandle(MethodBase method)
        {
            if (method is DynamicMethod)
            {
                FieldInfo fieldInfo = typeof(DynamicMethod).GetField("m_method", BindingFlags.NonPublic | BindingFlags.Instance);
                RuntimeMethodHandle handle = (RuntimeMethodHandle)fieldInfo?.GetValue(method);
                return handle;
            }

            return method.MethodHandle;
        }

        private static bool MethodSignaturesEqual(MethodBase x, MethodBase y)
        {    
            Type returnX = GetMethodReturnType(x), returnY = GetMethodReturnType(y);

            ParameterInfo[] xParams = x.GetParameters(), yParams = y.GetParameters();
            if (x.CallingConvention != y.CallingConvention ||returnX != returnY || xParams.Length != yParams.Length)
            {
                return false;
            }

            for (int i = 0; i < xParams.Length; i++)
            {
                if (xParams[i].ParameterType != yParams[i].ParameterType)
                {
                    return false;
                }
            }

            return true;
        }

        private static Type GetMethodReturnType(MethodBase method)
        {
            MethodInfo methodInfo = method as MethodInfo;

            if (methodInfo != null)
            {
                return methodInfo.ReturnType;
            }

            ConstructorInfo constructorInfo = method as ConstructorInfo;

            if (constructorInfo != null)
            {
                return typeof(void);
            }

            throw new ArgumentException("Unsupported MethodBase : " + method.GetType().Name, nameof(method));
        }

        public static T GetStaticField<T>(this Type type, string fieldName)
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return (T)field.GetValue(null);
        }

        private static readonly Dictionary<Type, Dictionary<string, FieldInfo>> _fieldInfoCache = [];

        private static FieldInfo GetFieldInfo(Type type, string fieldName)
        {
            if (!_fieldInfoCache.TryGetValue(type, out var fieldInfoDict))
            {
                fieldInfoDict = [];
                _fieldInfoCache[type] = fieldInfoDict;
            }

            if (!fieldInfoDict.TryGetValue(fieldName, out var fieldInfo))
            {
                fieldInfo = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                fieldInfo ??= type.GetField(GetBackingFieldName(fieldName), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                fieldInfoDict[fieldName] = fieldInfo;
            }

            return fieldInfo;
        }

        private static string GetBackingFieldName(string propertyName)
        {
            return string.Format($"<{propertyName}>>k__BackingField");
        }

        public static void SetFieldValue(Type type, string fieldName, object val)
        {
            FieldInfo fieldInfo = GetFieldInfo(type, fieldName) ?? throw new ArgumentOutOfRangeException("fieldName", $"Couldn't find field {fieldName} in type {type.FullName}");
            fieldInfo.SetValue(type, val);
        }
         public static void SetFieldValue<T>(string fieldName, object val)
        {
            Type type = typeof(T);
            FieldInfo fieldInfo = GetFieldInfo(type, fieldName) ?? throw new ArgumentOutOfRangeException("fieldName", $"Couldn't find field {fieldName} in type {type.FullName}");
            fieldInfo.SetValue(type, val);
        }

        public static void SetObjectFieldValue(object obj, string fieldName, object val)
        {
            Type type = obj.GetType();
            FieldInfo fieldInfo = GetFieldInfo(type, fieldName) ?? throw new ArgumentOutOfRangeException("fieldName", $"Couldn't find field {fieldName} in type {type.FullName}");
            fieldInfo.SetValue(obj, val);
        }

        public static object CreateInstance(Type type)
        {
            ConstructorInfo Ctor = type.GetConstructors().First();
            IEnumerable<object> parameters = from parameter in Ctor.GetParameters()
                                             select CreateInstance(parameter.ParameterType);
            return Activator.CreateInstance(type, [.. parameters]);
        }

        public static T ConstructPrivateClass<T>(Type[] argumentTypes, object[] parameters)
        {
            ConstructorInfo constructorInfo = typeof(T).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, argumentTypes, null);
            return (T)constructorInfo.Invoke(parameters);
        }

        public static void ConstructField(object obj, string fieldName)
        {     Type objectType = obj.GetType();
            FieldInfo field = objectType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (obj == null|| field == null)
            {
                return;
            }

            object val = CreateInstance(field.FieldType);
            field.SetValue(obj, val);
        }

        public static FieldInfo GetField<T>(string fieldName, BindingFlags bindingFlags, object obj = null, object value = null)
        {
            FieldInfo field = typeof(T).GetField(fieldName, bindingFlags);
            field?.SetValue(obj, value);
            return field;
        }

         public static FieldInfo GetField<T>(string fieldName, object obj = null, object value = null)
        {
            FieldInfo field = typeof(T).GetField(fieldName);
            field?.SetValue(obj, value);
            return field;
        }

        public static Type GetField(Type type, string fieldName, BindingFlags bindingFlags, object obj = null, object value = null)
        {
            FieldInfo field = type.GetField(fieldName, bindingFlags);
            field?.SetValue(obj, value);
            return field.FieldType;
        }

        public static FieldInfo GetTypeField<T>(string fieldName, BindingFlags bindingFlags, object obj = null, object value = null)
        {
            FieldInfo field = typeof(T).GetField(fieldName, bindingFlags);
            field?.SetValue(obj, value);
            return field;
        }


        //easy acccess reflection utilities
        
        public static Type Rtypeof<T>(string typeName)
        {
            return typeof(T).Assembly.GetType($"{typeof(T).Namespace}.{typeName}");
        }
        
        public static Type Rtypeof<T>(object obj)
        {
           return typeof(T).Assembly.GetType($"{typeof(T).Namespace}.{obj.GetType().Name}");
 
        }
        
        public static Type Rtypeof(Type type, object obj)
        {
            return type.Assembly.GetType($"{type.Namespace}.{obj.GetType().Name}");
        }

        public static Type Rtypeof(object obj)
        {
            return Type.GetType($"{obj.GetType().FullName}");
        }
        
        public static Type Ntypeof<T>(string type)
        {
            return typeof(T).GetNestedType(type);
        }

        public static Type Ntypeof(Type type, string nestedType, BindingFlags bindingFlags = BindingFlags.Default)
        {
           return type.GetNestedType(nestedType, bindingFlags);
        }
            

        public static Type Ntypeof<T>(string str, BindingFlags bindingFlags = BindingFlags.Default)
        {
             return typeof(T).GetNestedType(str, bindingFlags);
        }

        //underlying nullable
        public static Type UnullTypeof(Type type)
        { 
            return Nullable.GetUnderlyingType(type);
        }
        
        public static object UnullValueof<T>(object defaultValue, string typeName)
        {
            var underlyingType = Nullable.GetUnderlyingType(typeof(T));
            var property = underlyingType?.GetProperty(typeName);
            return property?.GetValue((T)defaultValue) is T value ? value : defaultValue ?? default(T);
        }

        //instance
    
         public static object Createinstof<T>(string typeName, params object[] parameters)
        {
            Type assemblyType = typeof(T).Assembly.GetType(typeName);
            object result = assemblyType != null ? Activator.CreateInstance(assemblyType, parameters) : null;
            return result;
        }

        public static object Createinstof(Type type, string typeName, params object[] parameters)
        {
            Type assemblyType = type.Assembly.GetType(typeName);
            return assemblyType != null ? Activator.CreateInstance(assemblyType, parameters) : null;
        }
        
    }
}

