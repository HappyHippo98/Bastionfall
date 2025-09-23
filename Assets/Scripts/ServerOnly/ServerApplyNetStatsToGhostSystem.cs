using Shared.Authoring.Monitoring;
using Shared.Authoring.Network;
using Shared.Rpc;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ServerOnly
{
    // Server empfängt Reports und schreibt NetStats in den passenden Player-Ghost (Owner Match).
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(RpcSystem))]
    [UpdateAfter(typeof(ServerSpawnPlayerOnConnectSystem))]
    [BurstCompile]
    public partial struct ServerApplyNetStatsToGhostSystem : ISystem
    {
        private EntityQuery _reportsQ;
        private EntityQuery _connsQ;
        private EntityQuery _playersQ;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableNetcode>();

            _reportsQ = state.GetEntityQuery(
                ComponentType.ReadOnly<ReportNetStatsRpc>(),
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

            var em = state.EntityManager;
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            // Mappe OwnerId -> RTT (vom Server gemessen)
            using var ids = _connsQ.ToComponentDataArray<NetworkId>(state.WorldUpdateAllocator);
            using var acks = _connsQ.ToComponentDataArray<NetworkSnapshotAck>(state.WorldUpdateAllocator);
            var rttById = new NativeParallelHashMap<int, float>(ids.Length, state.WorldUpdateAllocator);
            for (var i = 0; i < ids.Length; i++)
                rttById.TryAdd(ids[i].Value, acks[i].EstimatedRTT);

            // Alle Player-Ghosts mit Owner einsammeln
            using var owners = _playersQ.ToComponentDataArray<GhostOwner>(state.WorldUpdateAllocator);
            using var ents = _playersQ.ToEntityArray(state.WorldUpdateAllocator);

            foreach (var (rep, req, rpcEnt) in SystemAPI
                         .Query<RefRO<ReportNetStatsRpc>, RefRO<ReceiveRpcCommandRequest>>()
                         .WithEntityAccess())
            {
                // Sicheren Owner bestimmen: immer von der RPC-SourceConnection ableiten
                var ownerId = -1;
                if (em.HasComponent<NetworkId>(req.ValueRO.SourceConnection))
                    ownerId = em.GetComponentData<NetworkId>(req.ValueRO.SourceConnection).Value;

                // RTT bevorzugt aus Serverseite messen
                var rttMs = rep.ValueRO.RttMs;
                if (ownerId != -1 && rttById.TryGetValue(ownerId, out var srvRtt))
                    rttMs = srvRtt;

                // passenden Player-Ghost (Owner==ownerId) suchen & schreiben
                if (ownerId != -1)
                    for (var i = 0; i < ents.Length; i++)
                    {
                        if (owners[i].NetworkId != ownerId) continue;

                        var s = em.GetComponentData<PlayerNetStatsData>(ents[i]);
                        s.NetworkId = ownerId;
                        s.RttMs = rttMs;
                        s.Fps = rep.ValueRO.Fps;
                        s.ConnectionSeconds = rep.ValueRO.ConnectionSeconds;
                        em.SetComponentData(ents[i], s);
                        break;
                    }

                ecb.DestroyEntity(rpcEnt);
            }

            ecb.Playback(em);
        }
    }
}