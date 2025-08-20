// Assets/Scripts/ServerOnly/ServerLogOnConnectSystem.cs
#if UNITY_DEBUG
using Game.Shared.Util;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ServerOnly.Logging
{
    public struct ConnectionSeenTag : IComponentData {}
    public struct InGameLoggedTag : IComponentData {}

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct ServerLogOnConnectSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (conn, e) in SystemAPI
                         .Query<RefRO<NetworkStreamConnection>>()
                         .WithNone<ConnectionSeenTag>()
                         .WithEntityAccess())
            {
                state.LogInfo("Conn", "New connection entity {0} (waiting for NetworkId/handshake)", e.Index);
                ecb.AddComponent<ConnectionSeenTag>(e);
            }
            ecb.Playback(state.EntityManager);
        }
    }
}
#endif