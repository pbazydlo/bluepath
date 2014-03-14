namespace Bluepath.Framework
{
    using System;

    using Bluepath.Storage;

    public interface IBluepathCommunicationFramework
    {
        Guid ProcessEid { get; }

        IStorage Storage { get; }
    }
}
