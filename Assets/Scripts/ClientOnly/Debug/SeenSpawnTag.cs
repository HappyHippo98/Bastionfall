using Shared.Authoring.Network;
using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;

namespace ClientOnly.Debugging
{
    public struct SeenSpawnTag : IComponentData {}

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct ClientGhostSpawnLoggerSystem : ISystem
    {
        private EntityQuery _q;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableNetcode>();
            _q = state.GetEntityQuery(
                ComponentType.ReadOnly<GhostInstance>(),
                ComponentType.Exclude<Prefab>(),
                ComponentType.Exclude<SeenSpawnTag>()
            );
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_q.IsEmptyIgnoreFilter) return;

            var em  = state.EntityManager;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (gi, e) in SystemAPI.Query<RefRO<GhostInstance>>()
                         .WithNone<Prefab, SeenSpawnTag>()
                         .WithEntityAccess())
            {
                int ghostType = gi.ValueRO.ghostType;
                int ghostId   = gi.ValueRO.ghostId;
                int ownerNid  = em.HasComponent<GhostOwner>(e) ? em.GetComponentData<GhostOwner>(e).NetworkId : -1;

                UnityEngine.Debug.Log($"[CLIENT] Spawned Ghost: type={ghostType} id={ghostId} ownerNid={ownerNid}");
                ecb.AddComponent<SeenSpawnTag>(e);
            }

            ecb.Playback(em);
        }
    }
}