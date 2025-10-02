using BastionFall.Core.Shared.Authoring;
using BastionFall.Core.Shared.Time;
using BastionFall.Features.Time.Shared;
using BastionFall.Features.Time.Shared.Rpc;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace BastionFall.Features.Time.Server
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ServerNetcodeTickBridgeSystem))]
    [UpdateBefore(typeof(RpcSystem))]
    [BurstCompile]
    public partial struct ServerInGameClockSystem : ISystem
    {
        private uint _lastBroadcastServerTick;
        private EntityQuery _inGameConnQ;

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

            // Query EINMAL bauen (OnCreate ist nicht geburstet)
            _inGameConnQ = SystemAPI.QueryBuilder()
                .WithAll<NetworkId, NetworkStreamInGame, NetworkStreamConnection>()
                .Build();

            _lastBroadcastServerTick = 0;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState s)
        {
            var dt = SystemAPI.Time.DeltaTime;

            ref var cfg = ref SystemAPI.GetSingletonRW<InGameTickConfig>().ValueRW;
            ref var st = ref SystemAPI.GetSingletonRW<InGameTickState>().ValueRW;

            // Effective Speed (stapelbar)
            var effSpeed = TickDefaults.InGameSpeedMultiplier;
            st.Accumulator += dt * effSpeed;

            while (st.Accumulator + 1e-9 >= cfg.SecondsPerTick)
            {
                st.Tick++;
                st.Accumulator -= cfg.SecondsPerTick;
            }

            // Periodisches Broadcasten
            uint srvTick = 0;
            if (SystemAPI.TryGetSingleton<NetcodeTickState>(out var nts)) srvTick = nts.ServerTick;

            const uint broadcastEvery = 8;
            if (srvTick != 0 && srvTick / broadcastEvery != _lastBroadcastServerTick / broadcastEvery)
            {
                BroadcastSync(ref s, in st, in cfg);
                _lastBroadcastServerTick = srvTick;
            }
        }

        // NICHT static, nutzt die gecachte Query
        [BurstCompile]
        private void BroadcastSync(ref SystemState s, in InGameTickState st, in InGameTickConfig cfg)
        {
            var em = s.EntityManager;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            using var conns = _inGameConnQ.ToEntityArray(Allocator.Temp);
            for (var i = 0; i < conns.Length; i++)
            {
                var rpc = ecb.CreateEntity();
                ecb.AddComponent(rpc, new InGameTickSyncRpc
                {
                    Tick = st.Tick,
                    Accumulator = st.Accumulator,
                    SecondsPerTick = cfg.SecondsPerTick
                });
                ecb.AddComponent(rpc, new SendRpcCommandRequest { TargetConnection = conns[i] });
            }

            ecb.Playback(em);
            ecb.Dispose();
        }
    }
}