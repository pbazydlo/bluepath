namespace Bluepath.Extensions
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Serialization.Formatters.Binary;

    public static class MethodHandleSerializerExtensions
    {
        public static byte[] SerializeMethodHandle(this MethodBase method)
        {
            return SerializeMethodHandle(method.MethodHandle);
        }

        #region Generic SerializeMethodHandle overloads
        public static byte[] SerializeMethodHandle<TResult>(this Func<TResult> function)
        {
            return SerializeMethodHandle(function.Method.MethodHandle);
        }

        public static byte[] SerializeMethodHandle<T1, TResult>(this Func<T1, TResult> function)
        {
            return SerializeMethodHandle(function.Method.MethodHandle);
        }

        public static byte[] SerializeMethodHandle<T1, T2, TResult>(this Func<T1, T2, TResult> function)
        {
            return SerializeMethodHandle(function.Method.MethodHandle);
        }

        public static byte[] SerializeMethodHandle<T1, T2, T3, TResult>(this Func<T1, T2, T3, TResult> function)
        {
            return SerializeMethodHandle(function.Method.MethodHandle);
        }

        public static byte[] SerializeMethodHandle<T1, T2, T3, T4, TResult>(this Func<T1, T2, T3, T4, TResult> function)
        {
            return SerializeMethodHandle(function.Method.MethodHandle);
        }

        public static byte[] SerializeMethodHandle<T1, T2, T3, T4, T5, TResult>(this Func<T1, T2, T3, T4, T5, TResult> function)
        {
            return SerializeMethodHandle(function.Method.MethodHandle);
        }

        public static byte[] SerializeMethodHandle<T1, T2, T3, T4, T5, T6, TResult>(this Func<T1, T2, T3, T4, T5, T6, TResult> function)
        {
            return SerializeMethodHandle(function.Method.MethodHandle);
        }

        public static byte[] SerializeMethodHandle<T1, T2, T3, T4, T5, T6, T7, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, TResult> function)
        {
            return SerializeMethodHandle(function.Method.MethodHandle);
        }

        public static byte[] SerializeMethodHandle<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> function)
        {
            return SerializeMethodHandle(function.Method.MethodHandle);
        }
        #endregion

        public static MethodBase DeserializeMethodHandle(this byte[] methodHandle)
        {
            var runtimeMethodHandle = methodHandle.Deserialize<RuntimeMethodHandle>();
            var methodFromHandle = MethodBase.GetMethodFromHandle(runtimeMethodHandle);
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

        private static byte[] SerializeMethodHandle(RuntimeMethodHandle methodHandle)
        {
            return methodHandle.Serialize();
            //var formatter = new BinaryFormatter();
            //using (var stream = new MemoryStream())
            //{
            //    formatter.Serialize(stream, methodHandle);
            //    stream.Seek(0, SeekOrigin.Begin);
            //    var serializedMethodHandle = stream.GetBuffer();
            //    return serializedMethodHandle;
            //}
        }
    }
}