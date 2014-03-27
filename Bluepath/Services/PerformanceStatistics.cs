namespace Bluepath.Services
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    using Bluepath.Executor;

    [DataContract]
    public class PerformanceStatistics
    {
        [DataMember]
        public IDictionary<ExecutorState, int> NumberOfTasks { get; set; } 
    }
}
