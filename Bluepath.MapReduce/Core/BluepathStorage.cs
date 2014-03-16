namespace Bluepath.MapReduce.Core
{
    using System;
    using System.Collections.Generic;

    using NetReduce.Core;

    public class BluepathStorage : IStorage
    {
        public BluepathStorage(Bluepath.Storage.IStorage storage)
        {
            
        }

        public IEnumerable<Uri> ListFiles()
        {
            throw new NotImplementedException();
        }

        public string Read(string fileName)
        {
            throw new NotImplementedException();
        }

        public string Read(Uri uri)
        {
            throw new NotImplementedException();
        }

        public string[] ReadLines(string fileName)
        {
            throw new NotImplementedException();
        }

        public void Store(string fileName, string value)
        {
            throw new NotImplementedException();
        }

        public void Store(Uri uri, string value)
        {
            throw new NotImplementedException();
        }

        public void Clean()
        {
            throw new NotImplementedException();
        }

        public string GetFileName(Uri uri)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetKeys()
        {
            throw new NotImplementedException();
        }

        public void Remove(Uri uri)
        {
            throw new NotImplementedException();
        }
    }
}
