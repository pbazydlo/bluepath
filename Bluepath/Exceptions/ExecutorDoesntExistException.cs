using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.Exceptions
{
    public class ExecutorDoesntExistException : ArgumentOutOfRangeException
    {
        public ExecutorDoesntExistException(string paramName, string message)
            : base(paramName, message)
        {

        }
    }
}
