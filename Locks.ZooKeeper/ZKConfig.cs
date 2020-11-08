using System;
using System.Collections.Generic;
using System.Text;

namespace Locks.ZooKeeper
{
    public class ZKConfig
    {
        public string ConnectionString { get; set; }
        public int SessionTimeout { get; set; } = 10000;
        public string Name { get; set; } = Guid.NewGuid().ToString("N");
        public string LockPath { get; set; } = "/locks";
    }
}
