namespace Bluepath.Services
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    public class RemoteExecutorServiceResult
    {
        [DataMember]
        public State ExecutorState { get; set; }

        [DataMember]
        public object Result { get; set; }

        [DataMember]
        public Exception Error { get; set; }

        [DataMember]
        public TimeSpan? ElapsedTime { get; set; }

        public enum State
        {
            NotStarted,
            Running,
            Finished,
            Faulted
        }
    }
}
