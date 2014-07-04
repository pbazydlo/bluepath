using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Bluepath.Extensions;
using Bluepath.Storage.Locks;
using Bluepath.Exceptions;

namespace Bluepath.Storage.Structures.Collections
{
    [Serializable]
    public class DistributedList<T> : IList<T>/* where T : new()*/
    {
        private string key;

        //[NonSerialized]
        protected IExtendedStorage storage;

        public DistributedList(IExtendedStorage storage, string key)
        {
            if (!typeof(T).IsSerializable)
                throw new InvalidOperationException("A serializable Type is required");

            this.Storage = storage;
            this.key = key;
            this.Initialize();
        }

        protected string LockKey
        {
            get
            {
                return string.Format("_listLock_{0}", this.Key);
            }
        }

        private string MetadataKey
        {
            get
            {
                return string.Format("_listMetadata_{0}", this.Key);
            }
        }

        public IExtendedStorage Storage
        {
            get { return this.storage; }
            set { this.storage = value; }
        }

        public string Key
        {
            get
            {
                return this.key;
            }
        }

        public int IndexOf(T item)
        {
            using (var @lock = this.Storage.AcquireLock(this.LockKey))
            {
                var metadata = this.GetMetadata();
                return this.InternalIndexOf(item, metadata);
            }
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            using (var @lock = this.Storage.AcquireLock(this.LockKey))
            {
                var metadata = this.GetMetadata();
                this.InternalRemoveAt(index, ref metadata);
            }
        }

        public T this[int index]
        {
            get
            {
                try
                {
                    return this.Storage.Retrieve<T>(this.GetItemKey(index));
                }
                catch (StorageKeyDoesntExistException ex)
                {
                    throw new IndexOutOfRangeException("It seems that index was out of range, check inner exception for details.", ex);
                }
            }
            set
            {
                using (var @lock = this.Storage.AcquireLock(this.LockKey))
                {
                    this.InternalSet(index, value);
                }
            }
        }

        public void Add(T item)
        {
            this.AddRange(new T[] { item });
        }

        public void AddRange(IEnumerable<T> items)
        {
            using (var @lock = this.Storage.AcquireLock(this.LockKey))
            {
                var metadata = this.GetMetadata();
                this.InternalAddRange(items.ToArray(), metadata);

                this.SetMetadata(metadata);
            }
        }

        public void Clear()
        {
            using (var @lock = this.Storage.AcquireLock(this.LockKey))
            {
                var metadata = this.GetMetadata();
                string[] keysToRemove = new string[metadata.Count];
                for (int i = 0; i < metadata.Count; i++)
                {
                    keysToRemove[i] = this.GetItemKey(i);
                }

                this.Storage.BulkRemove(keysToRemove);
                metadata.Count = 0;
                this.SetMetadata(metadata);
            }
        }

        public bool Contains(T item)
        {
            return this.IndexOf(item) != -1;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            using (var @lock = this.Storage.AcquireLock(this.LockKey))
            {
                var metadata = this.GetMetadata();
                if ((array.Length - arrayIndex) >= metadata.Count)
                {
                    var keysToRead = new string[metadata.Count];
                    for (int i = 0; i < metadata.Count; i++)
                    {
                        keysToRead[i] = this.GetItemKey(i);
                    }

                    var values = this.Storage.BulkRetrieve<T>(keysToRead);
                    for (int i = 0; i < metadata.Count; i++)
                    {
                        array[i + arrayIndex] = values[i];
                    }
                }
            }
        }

        public void CopyPartTo(int startIndex, int count, T[] array)
        {
            using (var @lock = this.Storage.AcquireLock(this.LockKey))
            {
                var metadata = this.GetMetadata();
                if ((startIndex + count) > metadata.Count)
                {
                    throw new IndexOutOfRangeException(string.Format("End index must not exceed element Count."));
                }

                var keysToRead = new string[count];
                for (int i = startIndex; i < startIndex + count; i++)
                {
                    keysToRead[i - startIndex] = this.GetItemKey(i);
                }

                var values = this.Storage.BulkRetrieve<T>(keysToRead);
                values.CopyTo(array, 0);
            }
        }

        public int Count
        {
            get { return this.GetMetadata().Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            using (var @lock = this.Storage.AcquireLock(this.LockKey))
            {
                var metadata = this.GetMetadata();
                var index = this.InternalIndexOf(item, metadata);
                this.InternalRemoveAt(index, ref metadata);

                return true;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new DistributedListEnumerator<T>(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private void Initialize()
        {
            try
            {
                this.SetMetadata(new DistributedListMetadata()
                    {
                        Count = 0
                    }, initialize: true);
            }
            catch (StorageKeyAlreadyExistsException)
            {
                // if already exists, read metadata (for type checking)
                this.GetMetadata();
            }
        }

        private string GetItemKey(int index)
        {
            return string.Format("_listItem_{0}_{1}", index, this.Key);
        }

        private void SetMetadata(DistributedListMetadata metadata, bool initialize = false)
        {
            if (initialize)
            {
                this.Storage.Store(this.MetadataKey, metadata);
            }
            else
            {
                this.Storage.Update(this.MetadataKey, metadata);
            }
        }

        private DistributedListMetadata GetMetadata()
        {
            return this.Storage.Retrieve<DistributedListMetadata>(this.MetadataKey);
        }

        private void InternalRemoveAt(int index, ref DistributedListMetadata metadata)
        {
            if (index >= metadata.Count)
            {
                throw new IndexOutOfRangeException();
            }

            for (int i = index; i < metadata.Count - 1; i++)
            {
                this.InternalSet(index, this[i + 1]);
            }

            this.Storage.Remove(this.GetItemKey(metadata.Count - 1));
            metadata.Count--;
            this.SetMetadata(metadata);
        }

        private int InternalIndexOf(T item, DistributedListMetadata metadata)
        {
            var itemHashCode = EqualityComparer<T>.Default.GetHashCode(item);
            for (int i = 0; i < metadata.Count; i++)
            {
                if (EqualityComparer<T>.Default.GetHashCode(this[i]) == itemHashCode)
                {
                    return i;
                }
            }

            return -1;
        }

        private void InternalSet(int index, T value)
        {
            this.Storage.Update(this.GetItemKey(index), value);
        }

        private void InternalAddRange(T[] items, DistributedListMetadata metadata)
        {
            var itemsWithKeys = items.Select(i =>
                {
                    var itemIndex = metadata.Count;
                    metadata.Count++;
                    return new KeyValuePair<string, T>(this.GetItemKey(itemIndex), i);
                }).ToArray();

            this.Storage.BulkStore(itemsWithKeys);
        }

        // TODO: Could contain some kind of index for faster search and index find operations
        [Serializable]
        private class DistributedListMetadata
        {
            public int Count { get; set; }
        }

        public class DistributedListEnumerator<X> : IEnumerator<X>/* where X : new()*/
        {
            private DistributedList<X> list;
            private int currentIndex;
            private X currentItem;
            private IStorageLock listLock;

            public DistributedListEnumerator(DistributedList<X> list)
            {
                this.listLock = list.Storage.AcquireLock(list.LockKey);
                this.list = list;
                this.currentIndex = -1;
                this.currentItem = default(X);
            }

            public X Current
            {
                get { return this.currentItem; }
            }

            public void Dispose()
            {
                this.listLock.Release();
            }

            object System.Collections.IEnumerator.Current
            {
                get { return this.Current; }
            }

            public bool MoveNext()
            {
                var cc = this.list.Count;
                this.currentIndex++;
                if (this.currentIndex >= this.list.Count)
                {
                    return false;
                }
                else
                {
                    this.currentItem = this.list[this.currentIndex];
                }

                return true;
            }

            public void Reset()
            {
                this.currentIndex = 0;
            }
        }
    }
}
