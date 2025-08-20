// Assets/Scripts/ServerOnly/ServerConnectionLoggingSystem.cs
#if UNITY_DEBUG
using Game.Shared.Util;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ServerOnly.Logging
{
    public struct LoggedNewConnection : IComponentData {}
    public struct LoggedNetworkId   : IComponentData {}
    public struct LoggedInGame      : IComponentData {}

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [CreateAfter(typeof(NetworkStreamReceiveSystem))]
    public partial struct ServerConnectionLoggingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.LogInfo("Bootstrap", fmt: "ServerConnectionLoggingSystem ONLINE");
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (_, e) in SystemAPI
                         .Query<RefRO<NetworkStreamConnection>>()
                         .WithNone<LoggedNewConnection>()
                         .WithEntityAccess())
            {
                ecb.AddComponent<LoggedNewConnection>(e);
                state.LogInfo("Conn", fmt: "New connection entity created (awaiting NetworkId).");
            }

            foreach (var (id, e) in SystemAPI
                         .Query<RefRO<NetworkId>>()
                         .WithAll<NetworkStreamConnection>()
                         .WithNone<LoggedNetworkId>()
                         .WithEntityAccess())
            {
                ecb.AddComponent<LoggedNetworkId>(e);
                state.LogInfo("Conn", fmt: $"Assigned NetworkId={id.ValueRO.Value}.");
            }

            foreach (var (id, e) in SystemAPI
                         .Query<RefRO<NetworkId>>()
                         .WithAll<NetworkStreamConnection, NetworkStreamInGame>()
                         .WithNone<LoggedInGame>()
                         .WithEntityAccess())
            {
                ecb.AddComponent<LoggedInGame>(e);
                state.LogInfo("Conn", fmt: $"Connection is InGame (netId={id.ValueRO.Value}).");
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
#endif
