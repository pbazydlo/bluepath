using Bluepath.Storage.Locks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.Storage
{
    interface IExtendedStorage : IStorage
    {
        IStorageLock AcquireLock(string key);

        bool AcquireLock(string key, TimeSpan timeout, out IStorageLock storageLock);

        void ReleaseLock(IStorageLock storageLock);
    }
}
