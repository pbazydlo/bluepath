using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.LogSaver
{
    class Program
    {
        static void Main(string[] args)
        {
            Bluepath.Log.RedisHost = args[2];
            Bluepath.Log.SaveXes(args[0], args[1], clearListAfterSave: true);
        }
    }
}
