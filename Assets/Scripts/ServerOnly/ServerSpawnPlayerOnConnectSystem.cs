using Shared.Authoring;
using Shared.Authoring.Network;
using Shared.Logging;
using Shared.Util.Timing;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace ServerOnly
{
    public struct PlayerSpawnedTag : IComponentData
    {
    }

    public struct PlayerSpawnLog : IComponentData
    {
        public Entity Player;
        public int OwnerNid;
    }

    [RunEveryServerTicks(30)]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ServerHandleInGameRpcSystem))]
    [BurstCompile]
    public partial struct ServerSpawnPlayerOnConnectSystem : ISystem
    {
        private RunEveryTicksGuard<ServerSpawnPlayerOnConnectSystem> _tickSystem;

        private EntityQuery _prefabQ;
        private EntityQuery _connQ;
        private EntityQuery _logQ;

        [BurstDiscard]
        public void OnCreate(ref SystemState state)
        {
            _tickSystem.InitFromAttribute();

            state.RequireForUpdate<EnableNetcode>();

            _prefabQ = state.GetEntityQuery(ComponentType.ReadOnly<PlayerPrefab>());
            _connQ = state.GetEntityQuery(
                ComponentType.ReadOnly<NetworkId>(),
                ComponentType.ReadOnly<NetworkStreamInGame>());

            _logQ = state.GetEntityQuery(ComponentType.ReadOnly<PlayerSpawnLog>());

            state.RequireForUpdate(_prefabQ);
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<NetworkTime>(out var nt)) return;
            var now = (long)nt.ServerTick.TickIndexForValidTick;
            if (!_tickSystem.IsDue(now)) return;


            var em = state.EntityManager;

            var prefabRef = _prefabQ.GetSingleton<PlayerPrefab>();
            var prefabExists = em.Exists(prefabRef.Prefab);
            var hasPrefabTag = prefabExists && em.HasComponent<Prefab>(prefabRef.Prefab);
            var hasGhostType = prefabExists && em.HasComponent<GhostType>(prefabRef.Prefab);
            DevLog.DebugSystem<ServerSpawnPlayerOnConnectSystem>("Server",
                $" Spawn pass. Prefab={prefabRef.Prefab} Exists={prefabExists} PrefabTag={hasPrefabTag} GhostType={hasGhostType}");

            // --- Phase 1: Spawn QUEUEN (nur ECB, nichts am EntityManager anfassen) ---
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var q = SystemAPI.QueryBuilder()
                .WithAll<NetworkId, NetworkStreamInGame>()
                .WithNone<PlayerSpawnedTag>()
                .Build();
            
            using var ents = q.ToEntityArray(state.WorldUpdateAllocator);
            using var ids  = q.ToComponentDataArray<NetworkId>(state.WorldUpdateAllocator);
            
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

                DevLog.DebugSystem<ServerSpawnPlayerOnConnectSystem>("Server",
                    $"Queue spawn -> deferred={player} for NID={nid.ValueRO.Value} (Playback pending)");
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
                    var ent = log.ValueRO.Player;
                    var expectedN = log.ValueRO.OwnerNid;

                    var exists = em.Exists(ent);
                    var ownerN = -1;
                    var hasOwner = exists && em.HasComponent<GhostOwner>(ent);
                    if (hasOwner) ownerN = em.GetComponentData<GhostOwner>(ent).NetworkId;

                    var hasLT = exists && em.HasComponent<LocalTransform>(ent);

                    DevLog.DebugSystem<ServerSpawnPlayerOnConnectSystem>("Server",
                        $"✅ Realized spawn entity={ent} Exists={exists} " +
                        $"Owner(set)={ownerN} (expected={expectedN}) HasLocalTransform={hasLT}");

                    ecb2.RemoveComponent<PlayerSpawnLog>(connEntity);
                }

                ecb2.Playback(em);
                ecb2.Dispose();
            }

            // --- Kurzer Audit ---------------------------------------------------
            var conns = _connQ.CalculateEntityCount();
            var ghostsWithOwner = em.CreateEntityQuery(ComponentType.ReadOnly<GhostOwner>()).CalculateEntityCount();
            DevLog.DebugSystem<ServerSpawnPlayerOnConnectSystem>("Server",
                $"Audit: InGameConnections={conns}, GhostsWithOwner={ghostsWithOwner}");

            // var rate = SystemAPI.GetSingleton<ClientServerTickRate>().SimulationTickRate;
            // _tickSystem.OverrideIntervalTicks(math.max(1, rate), ref state);
        }
    }
}