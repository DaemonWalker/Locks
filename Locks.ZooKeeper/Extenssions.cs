using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vostok.ZooKeeper.Client;
using Vostok.ZooKeeper.Client.Abstractions.Model;
using Vostok.ZooKeeper.Client.Abstractions.Model.Request;
using ZK = org.apache.zookeeper;

namespace Locks.ZooKeeper
{
    static class Extenssions
    {
        public static async Task Connect(this ZK.ZooKeeper zooKeeper)
        {
            while (zooKeeper.getState() == ZK.ZooKeeper.States.CONNECTING)
            {
                await Task.Delay(15);
            }
        }
        public static T GetResultSync<T>(this Task<T> task)
        {
            return task.ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public static T ToRequest<T>(this string path) where T : ZooKeeperRequest
        {
            return Activator.CreateInstance(typeof(T), path) as T;
        }
        public static byte[] ToBytes(this string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        public static int GetIndex(this string str)
        {
            return int.Parse(str.Split('-')[1]);
        }
    }
}
