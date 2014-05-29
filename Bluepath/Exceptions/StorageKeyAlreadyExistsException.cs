using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.Exceptions
{
    public class StorageKeyAlreadyExistsException : ArgumentOutOfRangeException
    {
        public StorageKeyAlreadyExistsException(string paramName, string message)
            : base(paramName, message)
        {

        }
    }
}
