using BastionFall.Core.Shared.Authoring;
using BastionFall.Core.Shared.Logging;
using BastionFall.Core.Shared.RPC;
using BastionFall.Core.Shared.Util.Timing;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace BastionFall.Core.Client
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(NetworkStreamConnectSystem))]
    [RunEveryServerTicks(30)]
    [BurstCompile]
    public partial struct ClientEnterGameSystem : ISystem
    {
        private EntityQuery _q;
        private RunEveryTicksGuard<ClientEnterGameSystem> _tickGuard;

        // Fallback für die Zeit-basierte Drosselung (solange ServerTick==0)
        private double _nextLocalAttemptSec;
        private int _intervalTicks;

        [BurstDiscard]
        public void OnCreate(ref SystemState s)
        {
            s.RequireForUpdate<EnableNetcode>();
            _q = s.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<NetworkId>(),
                ComponentType.ReadOnly<NetworkStreamConnection>(),
                ComponentType.Exclude<NetworkStreamInGame>());

            _tickGuard.InitFromAttribute();
            _intervalTicks = math.max(1, _tickGuard.IntervalTicks);
            _nextLocalAttemptSec = 0;
        }

        public void OnUpdate(ref SystemState s)
        {
            // Nichts tun, wenn keine „noch-nicht-InGame“-Connections existieren
            if (_q.IsEmptyIgnoreFilter) return;

            // 1) Takt bestimmen
            var haveNetTime = SystemAPI.TryGetSingleton<NetworkTime>(out var nt) && nt.ServerTick.SerializedData > 0;

            if (haveNetTime)
            {
                var nowTick = (long)nt.ServerTick.TickIndexForValidTick;
                if (!_tickGuard.IsDue(nowTick)) return;
            }
            else
            {
                // VOR dem ersten Snapshot: in Sekunden takten ~ „alle 30 Server-Ticks“
                var simHz = 30; // fallback
                if (SystemAPI.TryGetSingleton<ClientServerTickRate>(out var rate) && rate.SimulationTickRate > 0)
                    simHz = rate
                        .SimulationTickRate; // serverseitig stellst du das auf TickDefaults.SystemTicksPerSecond :contentReference[oaicite:3]{index=3}

                var intervalSec = (double)_intervalTicks / math.max(1, simHz);
                var nowSec = SystemAPI.Time.ElapsedTime;
                if (nowSec < _nextLocalAttemptSec) return;
                _nextLocalAttemptSec = nowSec + intervalSec;
            }

            // 2) Aktion: RPC senden + lokal InGame markieren (idempotent)
            var em = s.EntityManager;
            using var conns = _q.ToEntityArray(Allocator.Temp);
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var conn in conns)
            {
                var rpc = ecb.CreateEntity();
                ecb.AddComponent(rpc, new GoInGameRpc());
                ecb.AddComponent<SendRpcCommandRequest>(rpc);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                DevLog.InfoSystem<ClientEnterGameSystem>("Client", $"GoInGame queued (→Server). conn={conn}");
#endif
                if (!em.HasComponent<NetworkStreamInGame>(conn))
                    ecb.AddComponent<NetworkStreamInGame>(conn); // Standard-NetCode-Pattern
            }

            ecb.Playback(em);
            ecb.Dispose();
        }
    }
}