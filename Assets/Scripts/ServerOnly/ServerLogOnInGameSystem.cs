// Assets/Scripts/ServerOnly/ServerLogOnInGameSystem.cs
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Game.Shared.Util;

namespace Game.Server
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(ServerLogOnConnectSystem))]
    public partial struct ServerLogOnInGameSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (netId, e) in SystemAPI
                         .Query<RefRO<NetworkId>>()
                         .WithAll<NetworkStreamInGame>()
                         .WithNone<InGameLoggedTag>()
                         .WithEntityAccess())
            {
                state.LogInfo("Conn", "Connection InGame: netId={0}", netId.ValueRO.Value);
                ecb.AddComponent<InGameLoggedTag>(e);
            }
            ecb.Playback(state.EntityManager);
        }
    }
}