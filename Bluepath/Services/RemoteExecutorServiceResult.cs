namespace Bluepath.Services
{
    using System;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Text;

    using Bluepath.Executor;

    [DataContract]
    [KnownType(typeof(ArgumentException))]
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

                ex = new Exception(sb.ToString());

                // According to http://stackoverflow.com/a/7363321 we need to annotate this class with 'KnownType' 
                // attribute for every type of exception we want to serialize.
                // Although the Exception type is serializable, often whatever is set in its _data field 
                // is not serializable, and will sometimes cause a serialization issue. 
                // A workaround for this is to set the _data field to null before serializing.
                var fieldInfo = typeof(Exception).GetField("_data", BindingFlags.Instance | BindingFlags.NonPublic);
                var temp = ex;
                while (temp != null)
                {
                    fieldInfo.SetValue(temp, null);
                    temp = temp.InnerException;
                }

                this.error = ex;
            }
        }
    }
}
