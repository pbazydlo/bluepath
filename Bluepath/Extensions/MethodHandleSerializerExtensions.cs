namespace Bluepath.Extensions
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;
    using System.Text.RegularExpressions;

    public static class MethodHandleSerializerExtensions
    {
        public static byte[] SerializeMethodHandle(this MethodBase method)
        {
            return SerializeMethodHandle(method.MethodHandle, method.DeclaringType.TypeHandle);
        }

        #region Generic SerializeMethodHandle overloads
        public static byte[] SerializeMethodHandle<TResult>(this Func<TResult> function)
        {
            return SerializeMethodHandle(function.Method.MethodHandle, function.Method.DeclaringType.TypeHandle);
        }

        public static byte[] SerializeMethodHandle<T1, TResult>(this Func<T1, TResult> function)
        {
            return SerializeMethodHandle(function.Method.MethodHandle, function.Method.DeclaringType.TypeHandle);
        }

        public static byte[] SerializeMethodHandle<T1, T2, TResult>(this Func<T1, T2, TResult> function)
        {
            return SerializeMethodHandle(function.Method.MethodHandle, function.Method.DeclaringType.TypeHandle);
        }

        public static byte[] SerializeMethodHandle<T1, T2, T3, TResult>(this Func<T1, T2, T3, TResult> function)
        {
            return SerializeMethodHandle(function.Method.MethodHandle, function.Method.DeclaringType.TypeHandle);
        }

        public static byte[] SerializeMethodHandle<T1, T2, T3, T4, TResult>(this Func<T1, T2, T3, T4, TResult> function)
        {
            return SerializeMethodHandle(function.Method.MethodHandle, function.Method.DeclaringType.TypeHandle);
        }

        public static byte[] SerializeMethodHandle<T1, T2, T3, T4, T5, TResult>(this Func<T1, T2, T3, T4, T5, TResult> function)
        {
            return SerializeMethodHandle(function.Method.MethodHandle, function.Method.DeclaringType.TypeHandle);
        }

        public static byte[] SerializeMethodHandle<T1, T2, T3, T4, T5, T6, TResult>(this Func<T1, T2, T3, T4, T5, T6, TResult> function)
        {
            return SerializeMethodHandle(function.Method.MethodHandle, function.Method.DeclaringType.TypeHandle);
        }

        public static byte[] SerializeMethodHandle<T1, T2, T3, T4, T5, T6, T7, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, TResult> function)
        {
            return SerializeMethodHandle(function.Method.MethodHandle, function.Method.DeclaringType.TypeHandle);
        }

        public static byte[] SerializeMethodHandle<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> function)
        {
            return SerializeMethodHandle(function.Method.MethodHandle, function.Method.DeclaringType.TypeHandle);
        }
        #endregion

        public static MethodBase DeserializeMethodHandle(this byte[] methodHandle)
        {
            var methodHandleParts = methodHandle.Deserialize<MethodHandleParts>();
            var runtimeMethodHandle = methodHandleParts.MethodHandle;
            var runtimeTypeHandle = methodHandleParts.TypeHandle;
            var methodFromHandle = MethodBase.GetMethodFromHandle(runtimeMethodHandle, runtimeTypeHandle);
            return methodFromHandle;
        }

        public static byte[] Serialize<T>(this T obj)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, obj);
                stream.Seek(0, SeekOrigin.Begin);
                var serializedObject = stream.GetBuffer();
                return serializedObject;
            }
        }

        public static T Deserialize<T>(this byte[] serializedObject)
        {
            var result = default(T);
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                stream.Write(serializedObject, 0, serializedObject.Length);
                stream.Seek(0, SeekOrigin.Begin);
                result = (T)formatter.Deserialize(stream);
            }

            return result;
        }

        public static string ToReadableString(this byte[] bytes)
        {
            bytes = bytes.Where(b => b > 0x00).ToArray();
            var input = Encoding.UTF8.GetString(bytes);

            return Regex.Replace(input, @"\p{Cc}", a => string.Format("[{0:X2}]", (byte)a.Value[0]));
        }

        private static byte[] SerializeMethodHandle(RuntimeMethodHandle methodHandle, RuntimeTypeHandle typeHandle)
        {
            return new MethodHandleParts()
            {
                MethodHandle = methodHandle,
                TypeHandle = typeHandle
            }.Serialize();
        }

        [Serializable]
        public class MethodHandleParts
        {
            public RuntimeMethodHandle MethodHandle { get; set; }

            public RuntimeTypeHandle TypeHandle { get; set; }
        }
    }
}