using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.Exceptions
{
    public class StorageOperationException : Exception
    {
        public StorageOperationException(string message)
            : base(message)
        {

        }

        public StorageOperationException(string message, Exception innerException)
            : base(message,innerException)
        {

        }
    }
}
