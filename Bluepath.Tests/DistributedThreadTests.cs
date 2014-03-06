using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Shouldly;

namespace Bluepath.Tests
{
    [TestClass]
    public class DistributedThreadTests
    {
        [TestMethod]
        public void ExecutesSingleThread()
        {
            var listToProcess = new List<int>()
            {
                1,2,3,4,5,6,7,8,9
            };

            Func<List<int>, int, int, int, int?> t1Action = (list, start, stop, threshold) =>
                {
                    for (int i = start; i < stop; i++)
                    {
                        if (list[i] > threshold)
                        {
                            return list[i];
                        }
                    }

                    return null;
                };

            DistributedThread dt1 = DistributedThread.Create(
                (parameters) => t1Action(parameters[0] as List<int>, (int)parameters[1], (int)parameters[2], (int)parameters[3])
                );
            dt1.Start(new object[] { listToProcess, 0, listToProcess.Count, 5 });
            dt1.Join();

            Convert.ToInt32(dt1.Result).ShouldBe(6);
        }
    }
}
