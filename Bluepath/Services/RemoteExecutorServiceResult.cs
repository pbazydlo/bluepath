namespace Bluepath.Services
{
    using System;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Text;

    using Bluepath.Executor;

    [DataContract]
    public class RemoteExecutorServiceResult
    {
        private Exception error;

        [DataMember]
        public ExecutorState ExecutorState { get; set; }

        [DataMember]
        public object Result { get; set; }

        [DataMember]
        public TimeSpan? ElapsedTime { get; set; }

        [DataMember]
        public Exception Error
        {
            get
            {
                return this.error;
            }

            set
            {
                var sb = new StringBuilder();

                var ex = value;               
                while (ex != null)
                {
                    sb.AppendLine(ex.ToString());
                    sb.AppendLine("---");
                    ex = ex.InnerException;
                }

                this.error = new Exception(sb.ToString());
            }
        }
    }
}
