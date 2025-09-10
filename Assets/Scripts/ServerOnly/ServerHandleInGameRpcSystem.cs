using Shared.Authoring.Network;
using Shared.Rpc;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ServerOnly
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [BurstCompile]
    public partial struct ServerHandleInGameRpcSystem : ISystem
    {
        [BurstCompile] public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableNetcode>();
        }

        [BurstCompile] public void OnUpdate(ref SystemState state)
        {
            var q = SystemAPI.QueryBuilder()
                .WithAll<GoInGameRpc, ReceiveRpcCommandRequest>()
                .Build();

            var rpcEntities = q.ToEntityArray(Allocator.Temp);
            var requests    = q.ToComponentDataArray<ReceiveRpcCommandRequest>(Allocator.Temp);

            for (int i = 0; i < rpcEntities.Length; i++)
            {
                var conn = requests[i].SourceConnection;

                if (!state.EntityManager.HasComponent<NetworkStreamInGame>(conn))
                {
                    state.EntityManager.AddComponent<NetworkStreamInGame>(conn);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    UnityEngine.Debug.Log("[Server] Connection marked InGame.");
#endif
                }

                state.EntityManager.DestroyEntity(rpcEntities[i]);
            }
        }
    }
}