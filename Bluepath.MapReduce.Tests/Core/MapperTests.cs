﻿namespace NetReduce.Core.Tests
{
    using System;
    using System.Linq;

    using Bluepath.MapReduce;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Shouldly;

    [TestClass]
    public class MapperTests
    {
        private IMapReduceStorage storage;

        [TestInitialize]
        public void Init()
        {
            this.storage = new InMemoryStorage();
        }

        [TestMethod]
        public void MapperLoadsGivenSource()
        {
            var filePath = "file1.txt";
            var fileContent = "whatever";
            storage.Store(filePath, fileContent);

            var mapper = new Mapper(filePath, null, this.storage);

            mapper.Value.ShouldBe(fileContent);
        }

        [TestMethod]
        public void MapperPerformsMapOperation()
        {
            var filePath = "file1.txt";
            var fileContent = "whatever am i";
            storage.Store(filePath, fileContent);

            var mapper = new Mapper(filePath, (key, value) => 
            {
                var result = new System.Collections.Generic.SortedList<string, string>();
                foreach (var w in value.Split(' '))
                    result.Add(w, "1");

                return result;
            }, this.storage);

            var mapResult = mapper.PerformMap();

            mapResult.Count().ShouldBe(3);
        }

        [TestMethod]
        public void MapperPerformsMapOperationUsingExternalCode()
        {
            var filePath = "file1.txt";
            var fileContent = "whatever am i";
            storage.Store(filePath, fileContent);

            TestHelpers.LoadToStorage(@"..\..\SampleMapper.cs", new FileUri("file:///SampleMapper.cs"), this.storage);
            var mapProvider = Loader.Load<IMapProvider>("SampleMapper.cs",this.storage);
            var mapper = new Mapper(filePath, mapProvider.Map, this.storage);

            var mapResult = mapper.PerformMap();

            mapResult.Count().ShouldBe(3);
        }
    }
}
