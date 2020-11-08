using org.apache.zookeeper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static org.apache.zookeeper.Watcher.Event;

namespace Locks.ZooKeeper
{
    class WaitWatcher : Watcher
    {
        private readonly TaskCompletionSource<bool> tcs;
        private readonly string targetPath;
        private readonly object lockObj = new object();
        private readonly Func<WatchedEvent, bool> condition;
        public WaitWatcher(string targetPath, TaskCompletionSource<bool> tcs, Func<WatchedEvent, bool> condition)
        {
            this.targetPath = targetPath;
            this.tcs = tcs;
            this.condition = condition;
        }

        public override Task process(WatchedEvent e)
        {
            if (string.IsNullOrEmpty(targetPath) == false && targetPath != e.getPath())
            {
                return Task.FromResult(0);
            }
            if (condition(e))
            {
                lock (lockObj)
                {
                    tcs.TrySetCanceled();
                }
            }
            return Task.FromResult(0);
        }
    }
}
