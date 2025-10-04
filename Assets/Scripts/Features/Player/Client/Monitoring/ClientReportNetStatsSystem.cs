using BastionFall.Core.Shared.Authoring;
using BastionFall.Features.Player.Shared.Authoring.Monitoring;
using BastionFall.Features.Player.Shared.RPC;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
// EnableNetcode

namespace BastionFall.Features.Player.Client.Monitoring
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct ClientReportNetStatsSystem : ISystem
    {
        private double _next;
        private EntityQuery _connQ;
        private double _inGameSince;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableNetcode>();
            state.RequireForUpdate<NetStatsConfig>();

            _connQ = state.GetEntityQuery(
                ComponentType.ReadOnly<NetworkSnapshotAck>(),
                ComponentType.ReadOnly<NetworkId>(),
                ComponentType.ReadOnly<NetworkStreamConnection>());

            _next = 0;
            _inGameSince = -1;
        }

        public void OnUpdate(ref SystemState state)
        {
            var cfg = SystemAPI.GetSingleton<NetStatsConfig>();
            if (cfg.Active == 0) return;

            var hasConn = !_connQ.IsEmptyIgnoreFilter;
            var now = SystemAPI.Time.ElapsedTime;
            if (hasConn && _inGameSince < 0) _inGameSince = now;
            if (!hasConn) _inGameSince = -1;

            if (now < _next) return;
            _next = now + cfg.ReportInterval;

            if (!hasConn) return;

            var id = -1;
            var rttMs = -1f;
            using var acks = _connQ.ToComponentDataArray<NetworkSnapshotAck>(state.WorldUpdateAllocator);
            using var ids = _connQ.ToComponentDataArray<NetworkId>(state.WorldUpdateAllocator);
            if (acks.Length > 0)
            {
                rttMs = acks[0].EstimatedRTT;
                id = ids[0].Value;
            }

            var fps = 0;
            var dt = SystemAPI.Time.DeltaTime;
            if (dt > 0.0001f) fps = (int)math.floor(1 / dt);

            var connSeconds = _inGameSince >= 0 ? (int)math.floor(now - _inGameSince) : 0;

            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var e = ecb.CreateEntity();
            ecb.AddComponent(e,
                new ReportNetStatsRpc { NetworkId = id, RttMs = rttMs, Fps = fps, ConnectionSeconds = connSeconds });
            ecb.AddComponent(e, new SendRpcCommandRequest());
            ecb.Playback(state.EntityManager);
        }
    }
}