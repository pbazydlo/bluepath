namespace Bluepath.Framework
{
    using System;

    using Bluepath.Executor;
    using Bluepath.Storage;

    public class BluepathCommunicationFramework : IBluepathCommunicationFramework
    {
        private readonly ILocalExecutor executor;

        public BluepathCommunicationFramework(ILocalExecutor executor)
        {
            this.executor = executor;
        }

        public Guid ProcessEid
        {
            get
            {
                return this.executor.Eid;
            }
        }

        public IStorage Storage { get; private set; }
    }
}
