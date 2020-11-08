using org.apache.zookeeper;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Locks.ZooKeeper
{
    class DeleteWatcher : Watcher
    {
        private readonly CancellationTokenSource cts;
        public DeleteWatcher(CancellationTokenSource cts)
        {
            this.cts = cts;
        }
        public override Task process(WatchedEvent e)
        {
            if (e.get_Type() == Event.EventType.NodeDeleted)
            {
                cts.Cancel();
            }
            return Task.FromResult(0);
        }
    }
}
