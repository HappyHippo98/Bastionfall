// Assets/Scripts/ServerOnly/ServerGoInGameRpcSystem.cs
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Game.Shared.Net;
using Game.Shared.Util;

namespace Game.Server
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct ServerGoInGameRpcSystem : ISystem
    {
        public void OnUpdate(ref SystemState s)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (rpc, recv, e) in SystemAPI
                     .Query<RefRO<GoInGameRpc>, RefRO<ReceiveRpcCommandRequest>>()
                     .WithEntityAccess())
            {
                var conn = recv.ValueRO.SourceConnection;
                if (!s.EntityManager.HasComponent<NetworkStreamInGame>(conn))
                {
                    ecb.AddComponent<NetworkStreamInGame>(conn);
                    s.LogInfo("RPC", "Set InGame via RPC");
                }
                ecb.DestroyEntity(e);
            }

            ecb.Playback(s.EntityManager);
        }
    }
}
