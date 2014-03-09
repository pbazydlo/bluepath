namespace Bluepath.Services
{
    using System;
    using System.Runtime.Serialization;

    using Bluepath.Executor;

    [DataContract]
    public class RemoteExecutorServiceResult
    {
        [DataMember]
        public ExecutorState ExecutorState { get; set; }

        [DataMember]
        public object Result { get; set; }

        [DataMember]
        public Exception Error { get; set; }

        [DataMember]
        public TimeSpan? ElapsedTime { get; set; }
    }

    
}
