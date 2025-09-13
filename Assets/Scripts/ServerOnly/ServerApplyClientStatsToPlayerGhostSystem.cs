using Shared.Authoring.Monitoring;
using Shared.Authoring.Network;
using Shared.Rpc;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ServerOnly
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ServerApplyClientStatsToPlayerGhostSystem : ISystem
    {
        private EntityQuery _reportsQ;
        private EntityQuery _connsQ;
        private EntityQuery _playersQ;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableNetcode>();

            _reportsQ = state.GetEntityQuery(
                ComponentType.ReadOnly<PlayerNetStatsData>(),
                ComponentType.ReadOnly<ReceiveRpcCommandRequest>());

            _connsQ = state.GetEntityQuery(
                ComponentType.ReadOnly<NetworkId>(),
                ComponentType.ReadOnly<NetworkSnapshotAck>(),
                ComponentType.ReadOnly<NetworkStreamInGame>());

            _playersQ = state.GetEntityQuery(
                ComponentType.ReadOnly<PlayerNetStatsData>(),
                ComponentType.ReadOnly<GhostOwner>());
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_reportsQ.IsEmptyIgnoreFilter) return;

            var em  = state.EntityManager;
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            // Map: OwnerId -> serverseitig gemessene RTT (ms)
            using var ids  = _connsQ.ToComponentDataArray<NetworkId>(state.WorldUpdateAllocator);
            using var acks = _connsQ.ToComponentDataArray<NetworkSnapshotAck>(state.WorldUpdateAllocator);
            var rttById = new NativeParallelHashMap<int, float>(ids.Length, state.WorldUpdateAllocator);
            for (int i = 0; i < ids.Length; i++)
                rttById.TryAdd(ids[i].Value, acks[i].EstimatedRTT);

            // Player-Ghosts (mit Owner) einsammeln
            using var owners    = _playersQ.ToComponentDataArray<GhostOwner>(state.WorldUpdateAllocator);
            using var playerEnt = _playersQ.ToEntityArray(state.WorldUpdateAllocator);

            foreach (var (rep, req, rpcEnt) in SystemAPI
                         .Query<RefRO<ReportNetStatsRpc>, RefRO<ReceiveRpcCommandRequest>>()
                         .WithEntityAccess())
            {
                // Sicheren Owner aus der RPC-Quelle bestimmen
                int ownerId = -1;
                if (em.HasComponent<NetworkId>(req.ValueRO.SourceConnection))
                    ownerId = em.GetComponentData<NetworkId>(req.ValueRO.SourceConnection).Value;

                // RTT: preferiere Server-Messung, fallback auf vom Client mitgeschickte
                float rttMs = rep.ValueRO.RttMs;
                if (ownerId != -1 && rttById.TryGetValue(ownerId, out var serverRtt))
                    rttMs = serverRtt;

                // Passenden Player-Ghost finden und schreiben
                if (ownerId != -1)
                {
                    for (int i = 0; i < playerEnt.Length; i++)
                    {
                        if (owners[i].NetworkId != ownerId) continue;

                        var s = em.GetComponentData<PlayerNetStatsData>(playerEnt[i]);
                        s.NetworkId        = ownerId;
                        s.RttMs            = rttMs;
                        s.Fps              = rep.ValueRO.Fps;
                        s.ConnectionSeconds= rep.ValueRO.ConnectionSeconds;
                        em.SetComponentData(playerEnt[i], s);

                        break;
                    }
                }

                // RPC-Entity aufräumen
                ecb.DestroyEntity(rpcEnt);
            }

            ecb.Playback(em);
        }
    }
}
