using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.Storage.Structures
{
    public class DistributedCounter
    {
        private IExtendedStorage storage;
        private string id;

        /// <summary>
        /// Creates distributed counter.
        /// </summary>
        /// <param name="storage">Storage which will be used to save counter state and synchronize threads.</param>
        /// <param name="id">Unique counter identifier. All counters in the same storage with the same identifier share value.</param>
        public DistributedCounter(IExtendedStorage storage, string id, int value = 0)
        {
            this.storage = storage;
            this.id = id;
            this.Initialize(value);
        }

        private string LockId
        {
            get
            {
                return string.Format("dc{0}", this.Id);
            }
        }

        /// <summary>
        /// Object identifier in storage.
        /// </summary>
        public string Id
        {
            get
            {
                return this.id;
            }
        }

        /// <summary>
        /// Reads counter value from the storage (value can be outdated by the time it is returned!).
        /// </summary>
        /// <returns>Counter value read from the storage.</returns>
        public int GetValue()
        {
            return this.storage.Retrieve<int>(this.Id);
        }

        /// <summary>
        /// Sets counter to provided value.
        /// If many threads attempt to SetValue the LAST value saved will be used.
        /// If possible use Increase, or Decrease functions.
        /// </summary>
        /// <param name="value">New value for the counter.</param>
        public void SetValue(int value)
        {
            using(var @lock = this.storage.AcquireLock(this.LockId))
            {
                this.InternalSet(value);
            }
        }

        /// <summary>
        /// Increases counter by given amount.
        /// </summary>
        /// <param name="amount">Amount which will be added to counter value.</param>
        public void Increase(int amount = 1)
        {
            using(var @lock = this.storage.AcquireLock(this.LockId))
            {
                var newValue = this.GetValue() + amount;
                this.InternalSet(newValue);
            }
        }

        /// <summary>
        /// Decreases counter by given amount;
        /// </summary>
        /// <param name="amount">Amount which will be substracted from counter value.</param>
        public void Decrease(int amount = 1)
        {
            using (var @lock = this.storage.AcquireLock(this.LockId))
            {
                var newValue = this.GetValue() - amount;
                this.InternalSet(newValue);
            }
        }

        private void InternalSet(int value)
        {
            this.storage.Update(this.Id, value);
        }

        private void Initialize(int value)
        {
            try
            {
                this.storage.Store(this.Id, value);
            }
            catch (ArgumentOutOfRangeException)
            {
                // if already exists, ignore
            }
        }
    }
}
