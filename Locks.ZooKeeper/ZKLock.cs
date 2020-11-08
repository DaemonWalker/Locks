using Locks.Abstractions;
using org.apache.zookeeper;
using org.apache.zookeeper.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        //private readonly ZooKeeperClient zooKeeper;
        private readonly ZK.ZooKeeper zooKeeper;
        private readonly DummyWatcher watcher;
        private readonly List<ACL> acls = ZooDefs.Ids.OPEN_ACL_UNSAFE;
        private string nodePath;
        public ZKLock(ZKConfig config)
        {
            this.config = config;

            this.watcher = new DummyWatcher(this.config.Name);
            this.zooKeeper = new ZK.ZooKeeper(this.config.ConnectionString, this.config.SessionTimeout, this.watcher);
            //this.zooKeeper.Connect().Wait();
            while (zooKeeper.getState() == ZK.ZooKeeper.States.CONNECTING)
            {
                Thread.Sleep(15);
            }
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

            this.nodePath = await this.zooKeeper.createAsync(
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
            var cts = new CancellationTokenSource();
            await this.zooKeeper.getChildrenAsync($"{node}/{targetNode}", new DeleteWatcher(cts));
            await Task.Delay(Timeout.Infinite, cts.Token);

            Console.WriteLine($"{this.config.Name} Begin Lock");
            return;
        }

        public Task Unlock(string lockNode)
        {
            Console.WriteLine($"{this.config.Name} Release Lock");
            return this.zooKeeper.deleteAsync($"{config.LockPath}/${lockNode}/{this.nodePath}");
        }

        private async Task CreatNodeIfNotExist(string path)
        {
            var stat = await this.zooKeeper.existsAsync(path);
            if (stat == null)
            {
                await this.zooKeeper.createAsync(path, path.ToBytes(), this.acls, CreateMode.PERSISTENT);
            }
        }
    }
}
