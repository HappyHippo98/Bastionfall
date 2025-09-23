using Shared.Authoring.Network;
using Shared.Rpc;
using Shared.Time;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace ClientOnly.Timing
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(RpcSystem))]
    public partial struct ClientApplyInGameTickSyncSystem : ISystem
    {
        public void OnCreate(ref SystemState s)
        {
            s.RequireForUpdate<EnableNetcode>();

            if (!SystemAPI.TryGetSingleton<InGameTickConfig>(out _))
            {
                var e = s.EntityManager.CreateEntity(typeof(InGameTickConfig));
                s.EntityManager.SetComponentData(e,
                    new InGameTickConfig { SecondsPerTick = TickDefaults.InGameSecondsPerTick });
            }

            if (!SystemAPI.TryGetSingleton<InGameTickState>(out _))
            {
                var e = s.EntityManager.CreateEntity(typeof(InGameTickState));
                s.EntityManager.SetComponentData(e, new InGameTickState { Tick = 0, Accumulator = 0 });
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState s)
        {
            var q = SystemAPI.QueryBuilder()
                .WithAll<InGameTickSyncRpc, ReceiveRpcCommandRequest>()
                .Build();
            if (q.IsEmptyIgnoreFilter) return;

            var ecb = new EntityCommandBuffer(s.WorldUpdateAllocator);

            ref var cfg = ref SystemAPI.GetSingletonRW<InGameTickConfig>().ValueRW;
            ref var st = ref SystemAPI.GetSingletonRW<InGameTickState>().ValueRW;

            foreach (var (rpc, req, ent) in SystemAPI
                         .Query<RefRO<InGameTickSyncRpc>, RefRO<ReceiveRpcCommandRequest>>()
                         .WithEntityAccess())
            {
                cfg.SecondsPerTick = rpc.ValueRO.SecondsPerTick;
                st.Tick = rpc.ValueRO.Tick;
                st.Accumulator = rpc.ValueRO.Accumulator;

                ecb.DestroyEntity(ent);
            }

            ecb.Playback(s.EntityManager);
        }
    }
}