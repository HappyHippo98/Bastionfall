using Shared.Authoring;
using Shared.Authoring.Monitoring;
using Shared.Authoring.Network;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace ServerOnly
{
    // Marker auf Connection: „für diese Connection wurde schon gespawnt“
    public struct PlayerSpawnedTag : IComponentData {}

    // Temp-Logdaten, damit wir NACH Playback sauber loggen können
    public struct PlayerSpawnLog : IComponentData
    {
        public Entity Player;   // deferred -> wird beim Playback automatisch remapped
        public int OwnerNid;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ServerHandleInGameRpcSystem))]
    public partial struct ServerSpawnPlayerOnConnectSystem : ISystem
    {
        private EntityQuery _prefabQ;
        private EntityQuery _connQ;
        private EntityQuery _logQ;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableNetcode>();

            _prefabQ = state.GetEntityQuery(ComponentType.ReadOnly<PlayerPrefabRef>());
            _connQ   = state.GetEntityQuery(
                ComponentType.ReadOnly<NetworkId>(),
                ComponentType.ReadOnly<NetworkStreamInGame>());

            _logQ    = state.GetEntityQuery(ComponentType.ReadOnly<PlayerSpawnLog>());

            state.RequireForUpdate(_prefabQ);
        }

        public void OnUpdate(ref SystemState state)
        {
            var em    = state.EntityManager;
            var world = state.WorldUnmanaged.Name;

            // --- Prefab/Collection Diagnose -----------------------------------
            var prefabRef = _prefabQ.GetSingleton<PlayerPrefabRef>();
            bool prefabExists  = em.Exists(prefabRef.Prefab);
            bool hasPrefabTag  = prefabExists && em.HasComponent<Prefab>(prefabRef.Prefab);
            bool hasGhostType  = prefabExists && em.HasComponent<GhostType>(prefabRef.Prefab);
            Debug.Log($"[{world}] Spawn pass. Prefab={prefabRef.Prefab} Exists={prefabExists} PrefabTag={hasPrefabTag} GhostType={hasGhostType}");

            // --- Phase 1: Spawn QUEUEN (nur ECB, nichts am EntityManager anfassen) ---
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (nid, connEntity) in SystemAPI
                         .Query<RefRO<NetworkId>>()
                         .WithAll<NetworkStreamInGame>()
                         .WithNone<PlayerSpawnedTag>()
                         .WithEntityAccess())
            {
                var player = ecb.Instantiate(prefabRef.Prefab);

                // (Optional) Start-Transform
                ecb.SetComponent(player, LocalTransform.FromPositionRotationScale(
                    new float3(0, 0, 0), quaternion.identity, 1f));

                // Owner setzen
                ecb.SetComponent(player, new GhostOwner { NetworkId = nid.ValueRO.Value });

                // Temp-Log an Connection hängen (deferred Entity ist hier erlaubt!)
                ecb.AddComponent(connEntity, new PlayerSpawnLog { Player = player, OwnerNid = nid.ValueRO.Value });

                // „Schon gespawnt“ markieren, damit wir nicht doppelt spawnen
                ecb.AddComponent<PlayerSpawnedTag>(connEntity);

                Debug.Log($"[{world}] Queue spawn -> deferred={player} for NID={nid.ValueRO.Value} (Playback pending)");
            }

            // Realisieren
            ecb.Playback(em);
            ecb.Dispose();

            // --- Phase 2: Post-Playback-Logging & Aufräumen ---------------------
            if (!_logQ.IsEmptyIgnoreFilter)
            {
                var ecb2 = new EntityCommandBuffer(Allocator.Temp);

                foreach (var (log, connEntity) in SystemAPI
                             .Query<RefRO<PlayerSpawnLog>>()
                             .WithEntityAccess())
                {
                    var ent       = log.ValueRO.Player;     // jetzt REAL
                    int expectedN = log.ValueRO.OwnerNid;

                    bool exists   = em.Exists(ent);
                    int ownerN    = -1;
                    bool hasOwner = exists && em.HasComponent<GhostOwner>(ent);
                    if (hasOwner) ownerN = em.GetComponentData<GhostOwner>(ent).NetworkId;

                    bool hasLT    = exists && em.HasComponent<LocalTransform>(ent);

                    Debug.Log(
                        $"[{world}] ✅ Realized spawn entity={ent} Exists={exists} " +
                        $"Owner(set)={ownerN} (expected={expectedN}) HasLocalTransform={hasLT}");

                    // Temp-Log wieder entfernen
                    ecb2.RemoveComponent<PlayerSpawnLog>(connEntity);
                }

                ecb2.Playback(em);
                ecb2.Dispose();
            }

            // --- Kurzer Audit ---------------------------------------------------
            int conns      = _connQ.CalculateEntityCount();
            int ghostsWithOwner = em.CreateEntityQuery(ComponentType.ReadOnly<GhostOwner>()).CalculateEntityCount();
            Debug.Log($"[{world}] Audit: InGameConnections={conns}, GhostsWithOwner={ghostsWithOwner}");
        }
    }
}
