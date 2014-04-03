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

        public DistributedCounter(IExtendedStorage storage, string id)
        {
            this.storage = storage;
            this.id = id;
        }

        public int GetValue()
        {
            throw new NotImplementedException();
        }

        public void SetValue(int value)
        {
            throw new NotImplementedException();
        }

        public string Id
        {
            get
            {
                return this.id;
            }
        }

        public void Increase(int amount = 1)
        {
            throw new NotImplementedException();
        }

        public void Decrease(int amount = 1)
        {
            throw new NotImplementedException();
        }
    }
}
