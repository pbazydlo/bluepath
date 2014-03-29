namespace Bluepath.Tests.Service
{
    using System.Collections.Generic;

    using Bluepath.Services;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Shouldly;

    [TestClass]
    public class ServiceUriTests
    {
        [TestMethod]
        public void ServiceUriCanBeUsedForLookupAsKeyInTheDictionaryTest()
        {
            var dictionary = new Dictionary<ServiceUri, int>();

            dictionary.Add(new ServiceUri("http://localhost:1234/test.svc",  BindingType.BasicHttpBinding),  1);
            dictionary.Add(new ServiceUri("http://localhost:1234/test2.svc", BindingType.BasicHttpBinding),  2);
            dictionary.Add(new ServiceUri("http://localhost:1234/test2.svc", BindingType.BasicHttpsBinding), 3);

            var key1 = new ServiceUri("http://localhost:1234/test.svc", BindingType.BasicHttpBinding);
            dictionary.ContainsKey(key1).ShouldBe(true);
            var service1 = dictionary[key1];
            service1.ShouldBe(1);

            var key2 = new ServiceUri("http://localhost:1234/test2.svc", BindingType.BasicHttpBinding);
            dictionary.ContainsKey(key2).ShouldBe(true);
            var service2 = dictionary[key2];
            service2.ShouldBe(2);

            var key3 = new ServiceUri("http://localhost:1234/test2.svc", BindingType.BasicHttpsBinding);
            dictionary.ContainsKey(key3).ShouldBe(true);
            var service3 = dictionary[key3];
            service3.ShouldBe(3);
        }
    }
}
