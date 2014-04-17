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

    [Serializable]
    public class ComplexParameterWithFunc
    {
        public Func<string, string> Function { get; set; }

        public string Input { get; set; }
    }

    [Serializable]
    public class ComplexGenericParameter<TA, TB>
    {
        public TA SomeProperty { get; set; }

        public TB AnotherProperty { get; set; }
    }
}
