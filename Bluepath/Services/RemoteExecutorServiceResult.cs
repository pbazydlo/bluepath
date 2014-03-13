namespace Bluepath.Services
{
    using System;
    using System.Reflection;
    using System.Runtime.Serialization;

    using Bluepath.Executor;

    [DataContract]
    [KnownType(typeof(TargetInvocationException))]
    [KnownType(typeof(NullReferenceException))]
    public class RemoteExecutorServiceResult
    {
        private Exception error;

        [DataMember]
        public ExecutorState ExecutorState { get; set; }

        [DataMember]
        public object Result { get; set; }

        [DataMember]
        public TimeSpan? ElapsedTime { get; set; }

        // TODO: Test serialization of this field.
        [DataMember]
        public Exception Error
        {
            get
            {
                return this.error;
            }

            set
            {
                this.error = value;

                // According to http://stackoverflow.com/a/7363321 we need to annotate this class with 'KnownType' 
                // attribute for every type of exception we want to serialize.
                // Although the Exception type is serializable, often whatever is set in its _data field 
                // is not serializable, and will sometimes cause a serialization issue. See here. 
                // A workaround for this is to set the _data field to null before serializing.
                var ex = this.error;
                var fieldInfo = typeof(Exception).GetField("_data", BindingFlags.Instance | BindingFlags.NonPublic);
                while (ex != null)
                {
                    fieldInfo.SetValue(ex, null);
                    ex = ex.InnerException;
                }
            }
        }
    }
}
