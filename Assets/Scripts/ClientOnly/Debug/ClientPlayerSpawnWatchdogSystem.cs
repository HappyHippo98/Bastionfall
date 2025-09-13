using Shared.Authoring.Monitoring;
using Shared.Authoring.Network;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace ClientOnly.Debug
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [BurstCompile]
    public partial struct ClientPlayerSpawnWatchdogSystem : ISystem
    {
        private double _deadline;
        private bool _reported;
        private EntityQuery _connQ;
        private EntityQuery _playersQ;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableNetcode>();
            _connQ = state.GetEntityQuery(ComponentType.ReadOnly<NetworkId>(), ComponentType.ReadOnly<NetworkStreamInGame>());
            _playersQ = state.GetEntityQuery(ComponentType.ReadOnly<PlayerNetStatsData>(), ComponentType.ReadOnly<GhostOwner>());
            _deadline = -1;
            _reported = false;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            var world = state.WorldUnmanaged.Name;

            if (!_connQ.IsEmptyIgnoreFilter && _deadline < 0)
                _deadline = SystemAPI.Time.ElapsedTime + 2.0;  // 2s nach InGame warten

            if (_deadline < 0 || _reported || SystemAPI.Time.ElapsedTime < _deadline)
                return;

            int myId = -1;
            using (var ids = _connQ.ToComponentDataArray<NetworkId>(state.WorldUpdateAllocator))
                if (ids.Length > 0) myId = ids[0].Value;

            using var owners = _playersQ.ToComponentDataArray<GhostOwner>(state.WorldUpdateAllocator);
            using var ents   = _playersQ.ToEntityArray(state.WorldUpdateAllocator);

            bool foundMine = false;
            for (int i = 0; i < ents.Length; i++)
                if (owners[i].NetworkId == myId) { foundMine = true; break; }

            if (!foundMine)
            {
                var collQ = em.CreateEntityQuery(ComponentType.ReadOnly<GhostType>(), ComponentType.ReadOnly<Prefab>());
                int ghostPrefabs = collQ.CalculateEntityCount();

                UnityEngine.Debug.LogError(
                    $"[{world}] ❌ Kein eigener Player angekommen. Meine NID={myId}. " +
                    $"Players(total)={ents.Length}, GhostPrefabs={ghostPrefabs}. " +
                    $"Ursachen:\n - Prefab nicht in Ghost-Collection (siehe ProbeSystem).\n" +
                    $" - Server setzt anderen Owner.\n - Prefab ist kein Ghost.\n - Ghost hat keine replizierten Felder.");

                for (int i = 0; i < ents.Length; i++)
                    UnityEngine.Debug.Log($"[{world}]   Seen Player entity={ents[i]} ownerNID={owners[i].NetworkId}");
            }
            else
            {
                UnityEngine.Debug.Log($"[{world}] ✅ Eigener Player angekommen.");
            }

            _reported = true;
        }
    }
}
