using Locks.Abstractions;
using Locks.ZooKeeper;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Locks.Test
{
    class Program
    {
        static int orderId = 0;
        static ConcurrentQueue<int> ids = new ConcurrentQueue<int>();
        static void Main(string[] args)
        {

            var tasks = Enumerable.Range(1, 1000).Select(p =>
            {
                return Task.Run(async () => await Create());
            }).ToArray();
            Task.WaitAll(tasks);
            Console.WriteLine(ids.ToList().Distinct().Count());
        }
        static async Task Create()
        {
            try
            {
                var config = new ZKConfig()
                {
                    ConnectionString = "zk.daemonwow.com"
                };
                ILock l = new ZKLock(config);

                await l.Lock("test");
                var index = orderId;
                ids.Enqueue(index);
                orderId = index + 1;
                await l.Unlock("test");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
