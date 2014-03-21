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

        // TODO: Provide locks
        // Could be based on:
        //  Apache Zookeeper [https://github.com/ewhauser/zookeeper/tree/trunk/src/dotnet]
        //  Redis [https://github.com/ServiceStack/ServiceStack.Redis/wiki/RedisLocks] - s1 said that it is easy to deploy
        //  ZeroMQ [http://zeromq.org/]
    }
}
