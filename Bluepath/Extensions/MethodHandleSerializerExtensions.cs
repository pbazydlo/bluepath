namespace Bluepath.Extensions
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Serialization.Formatters.Binary;

    public static class MethodHandleSerializerExtensions
    {
        public static byte[] GetSerializedMethodHandle<TResult>(this Func<TResult> function)
        {
            return GetSerializedMethodHandle(function.Method.MethodHandle);
        }

        public static byte[] GetSerializedMethodHandle<T1, TResult>(this Func<T1, TResult> function)
        {
            return GetSerializedMethodHandle(function.Method.MethodHandle);
        }

        // TODO: Extend it like this? 
        // IExecutor.Initialize should then also accept parameters in this form
        // See RemoteExecutorServiceTests.RemoteExecutorServiceExecuteTest for sample usage
        public static byte[] GetSerializedMethodHandle<T1, T2, TResult>(this Func<T1, T2, TResult> function)
        {
            return GetSerializedMethodHandle(function.Method.MethodHandle);
        }

        public static MethodBase DeserializeMethodHandle(this byte[] methodHandle)
        {
            var methodFromHandle = default(MethodBase);
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                stream.Write(methodHandle, 0, methodHandle.Length);
                stream.Seek(0, SeekOrigin.Begin);
                var runtimeMethodHandle = (RuntimeMethodHandle)formatter.Deserialize(stream);
                methodFromHandle = MethodBase.GetMethodFromHandle(runtimeMethodHandle);
            }

            return methodFromHandle;
        }

        private static byte[] GetSerializedMethodHandle(RuntimeMethodHandle methodHandle)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, methodHandle);
                stream.Seek(0, SeekOrigin.Begin);
                var serializedMethodHandle = stream.GetBuffer();
                return serializedMethodHandle;
            }
        }
    }
}