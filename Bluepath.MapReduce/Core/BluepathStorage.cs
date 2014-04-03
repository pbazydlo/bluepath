namespace Bluepath.MapReduce.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using NetReduce.Core;
    using NetReduce.Core.Extensions;

    public class BluepathStorage : IMapReduceStorage
    {
        private const string fileListKey = "FILE-LIST--50A12BE9-B3E9-4787-BE15-A0C5576879A4";
        private const string fileListLockKey = "FILE-LIST-LOCK--FF64B91D-1479-4390-8CE0-60D3560C304B";

        private readonly Bluepath.Storage.IExtendedStorage storage;

        public BluepathStorage(Bluepath.Storage.IExtendedStorage storage)
        {
            this.storage = storage;

            try
            {
                this.storage.Retrieve<string[]>(fileListKey);
            }
            catch (ArgumentOutOfRangeException)
            {
                this.storage.Store(fileListKey, new string[] { });
            }
        }

        public BluepathStorage(Bluepath.Storage.IExtendedStorage storage, bool eraseContents)
            : this(storage)
        {
            if (eraseContents)
            {
                this.Clean();
            }
        }

        public IEnumerable<Uri> ListFiles()
        {
            var list = default(IEnumerable<Uri>);
            using (this.storage.AcquireLock(fileListLockKey))
            {
                list = this.storage.Retrieve<string[]>(fileListKey).Select(f => new Uri(string.Format("file:///{0}", f)));
            }

            return list;
        }

        public string Read(string fileName)
        {
            if (fileName.StartsWith("file:///"))
            {
                fileName = this.GetFileName(new Uri(fileName));
            }

            return this.storage.Retrieve<string>(fileName);
        }

        public string Read(Uri uri)
        {
            return this.Read(this.GetFileName(uri));
        }

        public string[] ReadLines(string fileName)
        {
            return this.Read(fileName).Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public void Store(string fileName, string value)
        {
            if (fileName.StartsWith("file:///"))
            {
                fileName = this.GetFileName(new Uri(fileName));
            }

            this.storage.StoreOrUpdate(fileName, value);

            using (this.storage.AcquireLock(fileListLockKey))
            {
                var list = this.storage.Retrieve<string[]>(fileListKey).ToList();
                list.Add(fileName);
                this.storage.StoreOrUpdate(fileListKey, list.ToArray());
            }
        }

        public void Store(Uri uri, string value)
        {
            this.Store(this.GetFileName(uri), value);
        }

        public void Clean()
        {
            var list = default(List<string>);
            using (this.storage.AcquireLock(fileListLockKey))
            {
                list = this.storage.Retrieve<string[]>(fileListKey).ToList();
                this.storage.StoreOrUpdate(fileListKey, new string[] { });
            }

            foreach (var file in list)
            {
                try
                {
                    this.storage.Remove(file);
                }
                catch
                {
                }
            }
        }

        public string GetFileName(Uri uri)
        {
            return BluepathStorage.GetFileNameStatic(uri);
        }

        public static string GetFileNameStatic(Uri uri)
        {
            var fileName = uri.Segments.Last();
            
            return fileName;
        }

        public IEnumerable<string> GetKeys()
        {
            var result = new List<string>();
            var regex = new Regex(string.Format("^" + Bluepath.MapReduce.Properties.Settings.Default.MapOutputFileName + "$", @"(?<Key>.+)", "[0-9]+", RegexExtensions.GuidRegexString));
            var uris = this.ListFiles();
            foreach (var uri in uris)
            {
                var fileName = this.GetFileName(uri);
                if (regex.IsMatch(fileName))
                {
                    var key = regex.Match(fileName).Groups["Key"].Value;
                    if (!result.Contains(key))
                    {
                        result.Add(key);
                    }
                }
            }

            return result;
        }

        public void Remove(Uri uri)
        {
            var fileName = this.GetFileName(uri);

            using (this.storage.AcquireLock(fileListLockKey))
            {
                var list = this.storage.Retrieve<string[]>(fileListKey).ToList();
                list.Remove(fileName);
                this.storage.StoreOrUpdate(fileListKey, list.ToArray());
            }

            this.storage.Remove(fileName);
        }
    }
}
