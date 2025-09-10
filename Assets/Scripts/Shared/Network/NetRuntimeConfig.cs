using Unity.Entities;
using Unity.Networking.Transport;

namespace Shared.Network
{
    public struct NetRuntimeConfig : IComponentData
    {
        public NetworkEndpoint ServerEndpoint;
        public ushort Port;
        public float RetrySeconds;
    }
}