using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.Storage.Locks
{
    public interface IStorageLock : IDisposable
    {
        string Key { get; }

        bool IsAcquired { get; }

        bool Acquire();
        
        bool Acquire(TimeSpan? timeout);

        void Release();
    }
}
