using Shared;
using Shared.Authoring.Network;
using Shared.Logging;
using Shared.Rpc;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ServerOnly
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(RpcSystem))]
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

            var count = q.CalculateEntityCount();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Shared.Logging.DevLog.InfoSystem<ServerHandleInGameRpcSystem>(
                "Server", $"GoInGame: pending RPCs = {count}");
#endif
            if (count == 0) return;

            var rpcEntities = q.ToEntityArray(Allocator.Temp);
            var requests    = q.ToComponentDataArray<ReceiveRpcCommandRequest>(Allocator.Temp);

            for (int i = 0; i < rpcEntities.Length; i++)
            {
                var conn = requests[i].SourceConnection;

                if (!state.EntityManager.HasComponent<NetworkStreamInGame>(conn))
                {
                    state.EntityManager.AddComponent<NetworkStreamInGame>(conn);
                    Shared.Logging.DevLog.InfoSystem<ServerHandleInGameRpcSystem>(
                        "Server", $"GoInGame: marked connection InGame (connEntity={conn})");
                }
                else
                {
                    Shared.Logging.DevLog.InfoSystem<ServerHandleInGameRpcSystem>(
                        "Server", $"GoInGame: connection already InGame (connEntity={conn})");
                }

                state.EntityManager.DestroyEntity(rpcEntities[i]);
            }
        }

    }
}