using Shared.Authoring.Monitoring;
using Shared.Authoring.Network;
using Shared.Rpc;
using Unity.Entities;
using Unity.IO.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.NetCode;

namespace ClientOnly
{
    
    
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation|WorldSystemFilterFlags.ThinClientSimulation)]
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

            // Track InGame Start
            bool hasConn = !_connQ.IsEmptyIgnoreFilter;
            var now = SystemAPI.Time.ElapsedTime;
            if (hasConn && _inGameSince < 0) _inGameSince = now;
            if (!hasConn) _inGameSince = -1;

            if (now < _next) return;
            _next = now + cfg.ReportInterval;

            if (!hasConn) return;

            // RTT/Id aus Ack wie gehabt
            int id = -1; float rttMs = -1f;
            var acks = _connQ.ToComponentDataArray<NetworkSnapshotAck>(state.WorldUpdateAllocator);
            var ids  = _connQ.ToComponentDataArray<NetworkId>(state.WorldUpdateAllocator);
            if (acks.Length > 0) { rttMs = acks[0].EstimatedRTT; id = ids[0].Value; }

            // FPS
            int fps = 0;
            var dt = SystemAPI.Time.DeltaTime;
            if (dt > 0.0001f) fps = (int)math.floor(1 / dt);

            // ConnSeconds
            int connSeconds = _inGameSince >= 0 ? (int)math.floor(now-_inGameSince) : 0;

            // RPC
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var e = ecb.CreateEntity();
            ecb.AddComponent(e, new ReportNetStatsRpc { NetworkId = id, RttMs = rttMs, Fps = fps, ConnectionSeconds = connSeconds });
            ecb.AddComponent(e, new SendRpcCommandRequest());
            ecb.Playback(state.EntityManager);
        }
    }
}
