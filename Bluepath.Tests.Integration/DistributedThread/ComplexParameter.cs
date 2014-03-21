using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.Tests.Integration.DistributedThread
{
    [Serializable]
    public class ComplexParameter
    {
        public string SomeProperty { get; set; }

        public int AnotherProperty { get; set; }
    }
}
