using Shared.Authoring.Network;
using Shared.Rpc;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ClientOnly
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [BurstCompile]
    public partial struct ClientSendInGameRpcSystem : ISystem
    {
        private bool _sentForCurrentConnection;

        [BurstCompile] public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableNetcode>();
            _sentForCurrentConnection = false;
        }

        [BurstCompile] public void OnUpdate(ref SystemState state)
        {
            var q = SystemAPI.QueryBuilder()
                .WithAll<NetworkId, NetworkStreamConnection>()
                .Build();

            var conns = q.ToEntityArray(Allocator.Temp);
            if (conns.Length == 0)
            {
                _sentForCurrentConnection = false;
                return;
            }

            if (_sentForCurrentConnection) return;

            foreach (var conn in conns)
            {
                if (!state.EntityManager.HasComponent<NetworkStreamInGame>(conn))
                {
                    var rpc = state.EntityManager.CreateEntity();
                    state.EntityManager.AddComponentData(rpc, new GoInGameRpc());
                    state.EntityManager.AddComponentData(rpc, new SendRpcCommandRequest { TargetConnection = conn });

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    UnityEngine.Debug.Log("[Client] Sent GoInGameRpc.");
#endif
                    _sentForCurrentConnection = true;
                    break;
                }
            }
        }
    }
}