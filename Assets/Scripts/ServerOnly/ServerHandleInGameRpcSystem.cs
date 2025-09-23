using Shared.Authoring.Network;
using Shared.Rpc;
using Shared.Util.Timing;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ServerOnly
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(RpcSystem))]
    [RunEveryServerTicks(10)]
    [BurstCompile]
    public partial struct ServerHandleInGameRpcSystem : ISystem
    {
        private RunEveryTicksGuard<ServerHandleInGameRpcSystem> _tickSystem;

        [BurstDiscard]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableNetcode>();
            _tickSystem.InitFromAttribute();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<NetworkTime>(out var nt)) return;
            long now = nt.ServerTick.TickIndexForValidTick;
            if (!_tickSystem.IsDue(now)) return;


            var q = SystemAPI.QueryBuilder()
                .WithAll<GoInGameRpc, ReceiveRpcCommandRequest>()
                .Build();

            var count = q.CalculateEntityCount();
            if (count == 0) return;

            var rpcEntities = q.ToEntityArray(Allocator.Temp);
            var requests = q.ToComponentDataArray<ReceiveRpcCommandRequest>(Allocator.Temp);

            for (var i = 0; i < rpcEntities.Length; i++)
            {
                var conn = requests[i].SourceConnection;

                if (!state.EntityManager.HasComponent<NetworkStreamInGame>(conn))
                    state.EntityManager.AddComponent<NetworkStreamInGame>(conn);
                state.EntityManager.DestroyEntity(rpcEntities[i]);
            }
        }
    }
}