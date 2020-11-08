using org.apache.zookeeper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ZK = org.apache.zookeeper;

namespace Locks.ZooKeeper
{
    class DummyWatcher : ZK.Watcher
    {
        public string Name { get; }
        public DummyWatcher(string name)
        {
            this.Name = name;
        }
        public override Task process(WatchedEvent e)
        {
            var path = e.getPath();
            var state = e.getState();

            Console.WriteLine($"{Name} recieve: Path-{path}     State-{e.getState()}    Type-{e.get_Type()}");
            return Task.FromResult(0);
        }
    }
}
