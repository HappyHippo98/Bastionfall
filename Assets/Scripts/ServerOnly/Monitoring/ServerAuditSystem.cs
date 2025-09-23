﻿#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Shared.Authoring;
using Shared.Authoring.Network;
using Shared.Logging;
using Shared.Util.Timing;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ServerOnly.Monitoring
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ServerSpawnPlayerOnConnectSystem))]
    [RunEveryServerTicks(30)]
    public partial struct ServerAuditSystem : ISystem
    {
        private RunEveryTicksGuard<ServerAuditSystem> _tick;
        private EntityQuery _connQ;
        private EntityQuery _ownerQ;
        private EntityQuery _prefabQ;

        [BurstDiscard]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableNetcode>();
            _tick.InitFromAttribute();

            _connQ = state.GetEntityQuery(
                ComponentType.ReadOnly<NetworkId>(),
                ComponentType.ReadOnly<NetworkStreamInGame>());

            _ownerQ = state.GetEntityQuery(
                ComponentType.ReadOnly<GhostOwner>());

            _prefabQ = state.GetEntityQuery(
                ComponentType.ReadOnly<PlayerPrefab>());
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<NetworkTime>(out var nt)) return;
            var now = (long)nt.ServerTick.TickIndexForValidTick;
            if (!_tick.IsDue(now)) return;

            var em = state.EntityManager;

            // Counts
            var inGameConnCount = _connQ.CalculateEntityCount();
            var ghostsWithOwnerCount = _ownerQ.CalculateEntityCount();

            // Prefab Sanity
            var prefabOk = false;
            if (!_prefabQ.IsEmptyIgnoreFilter)
            {
                var pr = _prefabQ.GetSingleton<PlayerPrefab>().Prefab;
                prefabOk = em.Exists(pr) && em.HasComponent<GhostType>(pr);
            }

            // Mapping prüfen
            using var connIds = _connQ.ToComponentDataArray<NetworkId>(state.WorldUpdateAllocator);
            using var owners  = _ownerQ.ToComponentDataArray<GhostOwner>(state.WorldUpdateAllocator);

            var ownerSet = new NativeParallelHashSet<int>(owners.Length, state.WorldUpdateAllocator);
            for (var i = 0; i < owners.Length; i++) ownerSet.Add(owners[i].NetworkId);

            int missingOwners = 0;
            for (var i = 0; i < connIds.Length; i++)
                if (!ownerSet.Contains(connIds[i].Value))
                    missingOwners++;

            // Log (DEV)
            DevLog.DebugSystem<ServerAuditSystem>("Server",
                $"Audit: InGameConnections={inGameConnCount}, GhostsWithOwner={ghostsWithOwnerCount}, " +
                $"MissingOwnerForConn={missingOwners}, PrefabOk={prefabOk}");
        }
    }
}
#endif
