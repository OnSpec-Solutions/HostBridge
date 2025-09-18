using System;

namespace HostBridge.Examples.Common
{
    public interface IMyScoped
    {
        Guid Id { get; }
    }
    
    public sealed class MyScoped : IMyScoped
    {
        public Guid Id { get; } = Guid.NewGuid();
    }
}