using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.Exceptions
{
    public class DistributedDictionaryKeyAlreadyExistsException : ArgumentException
    {
        public DistributedDictionaryKeyAlreadyExistsException(string param, string message)
            : base(param, message)
        {

        }
    }
}
