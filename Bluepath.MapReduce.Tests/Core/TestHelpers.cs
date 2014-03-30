namespace NetReduce.Core.Tests
{
    using System;
    using System.IO;

    using Bluepath.MapReduce;

    public static class TestHelpers
    {
        public static void LoadToStorage(string realPath, FileUri storageFileName, IMapReduceStorage storage)
        {
            using (var sr = new StreamReader(realPath))
            {
                storage.Store(storage.GetFileName(storageFileName.Uri), sr.ReadToEnd());
            }
        }
    }
}
