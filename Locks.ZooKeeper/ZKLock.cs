using Locks.Abstractions;
using org.apache.zookeeper;
using org.apache.zookeeper.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static org.apache.zookeeper.Watcher.Event;
//using Vostok.ZooKeeper.Client;
//using Vostok.ZooKeeper.Client.Abstractions;
//using Vostok.ZooKeeper.Client.Abstractions.Model;
//using Vostok.ZooKeeper.Client.Abstractions.Model.Request;
using ZK = org.apache.zookeeper;

namespace Locks.ZooKeeper
{
    public class ZKLock : ILock
    {
        private readonly ZKConfig config;
        private static object lockObj = new object();
        private static ZK.ZooKeeper zooKeeper;
        private readonly List<ACL> acls = ZooDefs.Ids.OPEN_ACL_UNSAFE;
        private string nodePath;

        public ZKLock(ZKConfig config)
        {
            this.config = config;

            var tcs = new TaskCompletionSource<bool>();
            lock (lockObj)
            {
                if (zooKeeper == null)
                {
                    zooKeeper = new ZK.ZooKeeper(
                        this.config.ConnectionString,
                        this.config.SessionTimeout,
                        new WaitWatcher(string.Empty, tcs, e => e.getState() == KeeperState.SyncConnected));
                }
            }
            zooKeeper.Connect().Wait();
            if (zooKeeper.getState() != ZK.ZooKeeper.States.CONNECTED &&
                zooKeeper.getState() != ZK.ZooKeeper.States.CONNECTEDREADONLY)
            {
                throw new Exception("ZooKeeper Connected Failed");
            }
            CreatNodeIfNotExist(config.LockPath).Wait();

            Console.WriteLine($"{this.config.Name} Constructed");
        }
        public async Task Lock(string lockNode)
        {
            var node = $"{config.LockPath}/${lockNode}";
            await this.CreatNodeIfNotExist(node);
            var path = $"{config.LockPath}/${lockNode}/${config.Name}-";

            this.nodePath = await zooKeeper.createAsync(
                 path,
                 this.config.Name.ToBytes(),
                 this.acls,
                 ZK.CreateMode.EPHEMERAL_SEQUENTIAL);

            var index = nodePath.GetIndex();
            var children = await zooKeeper.getChildrenAsync(node);
            var childrenIndex = children.Children.Select(p => new { Node = p, Index = p.GetIndex() }).OrderBy(p => p.Index).ToList();

            if (childrenIndex.First().Index == index)
            {
                Console.WriteLine($"{this.config.Name} Begin Lock");
                return;
            }

            var targetNode = string.Empty;
            for (int i = 0; i < childrenIndex.Count; i++)
            {
                if (childrenIndex[i].Index == index)
                {
                    targetNode = childrenIndex[i - 1].Node;
                    break;
                }
            }
            if (string.IsNullOrEmpty(targetNode))
            {
                throw new Exception("Node Get Error");
            }
            var tcs = new TaskCompletionSource<bool>();
            Console.WriteLine($"{this.config.Name} Wait for {node}/{targetNode}");
            var waitNode = await zooKeeper.existsAsync(
                $"{node}/{targetNode}",
                new WaitWatcher($"{node}/{targetNode}", tcs, e => e.get_Type() == EventType.NodeDeleted || e.getState() == KeeperState.Disconnected));
            if (waitNode != null)
            {
                await Task.WhenAny(Task.Delay(Timeout.Infinite), tcs.Task);
            }
            Console.WriteLine($"{this.config.Name} Begin Lock");
            return;
        }

        public Task Unlock(string lockNode)
        {
            var path = $"{this.nodePath}";
            Console.WriteLine($"Unlock {path}");
            return zooKeeper.deleteAsync(path);
        }

        private async Task CreatNodeIfNotExist(string path)
        {
            var stat = await zooKeeper.existsAsync(path);
            if (stat == null)
            {
                await zooKeeper.createAsync(path, path.ToBytes(), this.acls, CreateMode.PERSISTENT);
            }
        }
    }
}
