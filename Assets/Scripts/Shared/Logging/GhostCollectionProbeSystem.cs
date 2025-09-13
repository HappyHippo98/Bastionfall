using Shared.Authoring;
using Shared.Authoring.Network;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Shared.Logging
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [BurstCompile]
    public partial struct GhostCollectionProbeSystem : ISystem
    {
        private EntityQuery _prefabQ;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableNetcode>();
            _prefabQ = state.GetEntityQuery(ComponentType.ReadOnly<PlayerPrefabRef>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            var world = state.WorldUnmanaged.Name;

            if (_prefabQ.IsEmptyIgnoreFilter)
            {
                Debug.LogError($"[{world}] ❌ Kein PlayerPrefabRef gefunden! (SubScene mit Authoring geladen?)");
                return;
            }

            var pr = _prefabQ.GetSingleton<PlayerPrefabRef>();
            bool exists  = em.Exists(pr.Prefab);
            bool hasPref = exists && em.HasComponent<Prefab>(pr.Prefab);
            bool hasGhost= exists && em.HasComponent<GhostType>(pr.Prefab);

            Debug.Log($"[{world}] PlayerPrefabRef -> {pr.Prefab}, Exists={exists}, PrefabTag={hasPref}, GhostType={hasGhost}");

            var gcQ = em.CreateEntityQuery(ComponentType.ReadOnly<GhostType>(), ComponentType.ReadOnly<Prefab>());
            using var ghosts = gcQ.ToEntityArray(Allocator.Temp);
            bool contains = false;
            for (int i = 0; i < ghosts.Length; i++) if (ghosts[i] == pr.Prefab) { contains = true; break; }
            Debug.Log($"[{world}] Ghost-Prefabs im World: {ghosts.Length} (>=1 erwartet). Enthält PlayerPrefab? {contains}");
        }
    }
}