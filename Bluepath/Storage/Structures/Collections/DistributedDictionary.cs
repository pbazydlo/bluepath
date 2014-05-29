using Bluepath.Exceptions;
using Bluepath.Storage.Locks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.Storage.Structures.Collections
{
    [Serializable]
    public class DistributedDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private string key;

        [NonSerialized]
        protected IExtendedStorage storage;

        protected IEqualityComparer<TKey> keyComparer;

        protected IEqualityComparer<TValue> valueComparer;

        public DistributedDictionary(
            IExtendedStorage storage,
            string key, 
            IEqualityComparer<TKey> keyComparer = null,
            IEqualityComparer<TValue> valueComparer = null
            )
        {
            this.Storage = storage;
            this.key = key;
            if(keyComparer==null)
            {
                this.keyComparer = EqualityComparer<TKey>.Default;
            }
            else
            {
                this.keyComparer = keyComparer;
            }

            if(valueComparer == null)
            {
                this.valueComparer = EqualityComparer<TValue>.Default;
            }
            else
            {
                this.valueComparer = valueComparer;
            }

            this.Initialize();
        }

        protected string LockKey
        {
            get
            {
                return string.Format("_ddictLock_{0}", this.Key);
            }
        }

        private string MetadataKey
        {
            get
            {
                return string.Format("_ddictMetadata_{0}", this.Key);
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

        public void Add(TKey key, TValue value)
        {
            using (var @lock = this.Storage.AcquireLock(this.LockKey))
            {
                var metadata = this.GetMetadata();
                if (this.InternalContainsKey(key, metadata))
                {
                    throw new DistributedDictionaryKeyAlreadyExistsException("Cannot add duplicate key!", "key");
                }

                metadata.Count++;
                metadata.Keys.Add(key);
                this.Storage.Store(this.GetItemStorageKey(key), value);
                this.SetMetadata(metadata);
            }
        }

        public bool ContainsKey(TKey key)
        {
            var metadata = this.GetMetadata();
            return this.InternalContainsKey(key, metadata);
        }

        public ICollection<TKey> Keys
        {
            get
            {
                var metadata = this.GetMetadata();
                return metadata.Keys;
            }
        }

        public bool Remove(TKey key)
        {
            using (var @lock = this.Storage.AcquireLock(this.LockKey))
            {
                var metadata = this.GetMetadata();
                this.Storage.Remove(this.GetItemStorageKey(key));
                metadata.Count--;
                metadata.Keys.Remove(key);
                this.SetMetadata(metadata);

                return true;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            try
            {
                value = this[key];
                return true;
            }
            catch
            {
                value = default(TValue);
                return false;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                List<TValue> values = new List<TValue>();
                foreach (var keyValuePair in this)
                {
                    values.Add(keyValuePair.Value);
                }

                return values;
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                return this.Storage.Retrieve<TValue>(this.GetItemStorageKey(key));
            }
            set
            {
                this.Storage.Update(this.GetItemStorageKey(key), value);
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            this.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            using (var @lock = this.Storage.AcquireLock(this.LockKey))
            {
                var metadata = this.GetMetadata();
                foreach (var key in metadata.Keys)
                {
                    this.Storage.Remove(this.GetItemStorageKey(key));
                }

                metadata.Count = 0;
                metadata.Keys.Clear();
                this.SetMetadata(metadata);
            }
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            var metadata = this.GetMetadata();
            if (this.InternalContainsKey(item.Key, metadata))
            {
                return this.valueComparer.GetHashCode(this[item.Key])
                    == this.valueComparer.GetHashCode(item.Value);
            }

            return false;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get
            {
                var metadata = this.GetMetadata();
                return metadata.Count;
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return this.Remove(item.Key);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return new DistributedDictionaryEnumerator<TKey, TValue>(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private void Initialize()
        {
            try
            {
                this.Storage.Store(this.MetadataKey, new DistributedDictionaryMetadata<TKey>());
            }
            catch (StorageKeyAlreadyExistsException)
            {
                this.GetMetadata();
            }
        }

        private DistributedDictionaryMetadata<TKey> GetMetadata()
        {
            return this.Storage.Retrieve<DistributedDictionaryMetadata<TKey>>(this.MetadataKey);
        }

        private void SetMetadata(DistributedDictionaryMetadata<TKey> metadata)
        {
            this.Storage.Update(this.MetadataKey, metadata);
        }

        private bool InternalContainsKey(TKey key, DistributedDictionaryMetadata<TKey> metadata)
        {
            return metadata.Keys.Contains(key);
        }

        private string GetItemStorageKey(TKey key)
        {
            return string.Format("_ddictItem_{0}_{1}", this.keyComparer.GetHashCode(key), this.Key);
        }

        [Serializable]
        private class DistributedDictionaryMetadata<XKey>
        {
            public DistributedDictionaryMetadata()
            {
                this.Keys = new HashSet<XKey>();
            }

            public HashSet<XKey> Keys { get; set; }

            public int Count { get; set; }
        }

        public class DistributedDictionaryEnumerator<XKey, XValue> : IEnumerator<KeyValuePair<XKey, XValue>>
        {
            private DistributedDictionary<XKey, XValue> dictionary;
            private ICollection<XKey> keys;
            private KeyValuePair<XKey, XValue> currentItem;
            private int currentIndex;
            private IStorageLock dictionaryLock;

            public DistributedDictionaryEnumerator(DistributedDictionary<XKey, XValue> dictionary)
            {
                this.dictionaryLock = dictionary.Storage.AcquireLock(dictionary.LockKey);
                this.dictionary = dictionary;
                this.keys = this.dictionary.Keys;
                this.currentIndex = -1;
                this.currentItem = default(KeyValuePair<XKey, XValue>);
            }

            public KeyValuePair<XKey, XValue> Current
            {
                get { return this.currentItem; }
            }

            public void Dispose()
            {
                this.dictionaryLock.Release();
            }

            object System.Collections.IEnumerator.Current
            {
                get { return this.Current; }
            }

            public bool MoveNext()
            {
                this.currentIndex++;
                if (this.currentIndex >= this.keys.Count)
                {
                    return false;
                }

                var key = this.keys.ElementAt(this.currentIndex);
                this.currentItem = new KeyValuePair<XKey, XValue>(key, this.dictionary[key]);
                return true;
            }

            public void Reset()
            {
                this.currentIndex = -1;
                this.currentItem = default(KeyValuePair<XKey, XValue>);
            }
        }
    }
}
