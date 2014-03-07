namespace Bluepath.Executor
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    public class MethodHandleSerializer
    {
        public static byte[] GetSerializedMethodHandle(Func<object[], object> function)
        {
            return GetSerializedMethodHandle(function.Method.MethodHandle);
        }

        public static byte[] GetSerializedMethodHandle<TResult>(Func<TResult> function)
        {
            return GetSerializedMethodHandle(function.Method.MethodHandle);
        }

        public static byte[] GetSerializedMethodHandle<T1, TResult>(Func<T1, TResult> function)
        {
            return GetSerializedMethodHandle(function.Method.MethodHandle);
        }

        // TODO: Extend it like this? 
        // IExecutor.Initialize should then also accept parameters in this form
        // See RemoteExecutorServiceTests.RemoteExecutorServiceExecuteTest for sample usage
        public static byte[] GetSerializedMethodHandle<T1, T2, TResult>(Func<T1, T2, TResult> function)
        {
            return GetSerializedMethodHandle(function.Method.MethodHandle);
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