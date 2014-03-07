using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.Services
{
    [DataContract]
    public class RemoteExecutorServiceResult
    {
        [DataMember]
        public State IsFinished { get; set; }

        [DataMember]
        public object Result { get; set; }

        [DataMember]
        public Exception Error { get; set; }

        [DataMember]
        public TimeSpan ElapsedTime { get; set; }

        public enum State
        {
            NotStarted,
            Running,
            Finished,
            Faulted
        }
    }
}
