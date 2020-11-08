using System;
using System.Threading.Tasks;

namespace Locks.Abstractions
{
    public interface ILock
    {
        Task Lock(string lockNode);
        Task Unlock(string lockNode);
    }
}
