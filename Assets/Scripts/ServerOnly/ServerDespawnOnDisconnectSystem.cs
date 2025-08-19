// Assets/Scripts/ServerOnly/ServerDespawnOnDisconnectSystem.cs
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using Game.Shared.Authoring; // PlayerTag
using Game.Shared.Config;   // NetGameSettings (ScriptableObject)

namespace Game.Server
{
    /// Entfernt Spieler-Ghosts, deren Besitzer nicht mehr InGame/aktiv ist.
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ServerDespawnOnDisconnectSystem : ISystem
    {
        bool _despawnEnabled;

        public void OnCreate(ref SystemState s)
        {
            // Settings aus Resources lesen (kein ECS-Singleton!)
            _despawnEnabled = true;
            var cfg = Resources.Load<NetGameSettings>("NetGameSettings");
            if (cfg) _despawnEnabled = cfg.DespawnOnDisconnect;
        }

        public void OnUpdate(ref SystemState s)
        {
            if (!_despawnEnabled) return;

            // 1) Aktive (InGame) Owner-NetIds einsammeln
            var activeIds = new NativeHashSet<int>(16, Allocator.Temp);
            foreach (var id in SystemAPI.Query<RefRO<NetworkId>>().WithAll<NetworkStreamInGame>())
                activeIds.Add(id.ValueRO.Value);

            // 2) Alle Player-Ghosts prüfen; Owner nicht aktiv? -> despawn
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (owner, e) in SystemAPI
                         .Query<RefRO<GhostOwner>>()
                         .WithAll<PlayerTag>()
                         .WithEntityAccess())
            {
                if (!activeIds.Contains(owner.ValueRO.NetworkId))
                {
                    ecb.DestroyEntity(e);
                    Debug.Log($"[Server] Despawn player (orphaned owner netId={owner.ValueRO.NetworkId}).");
                }
            }

            ecb.Playback(s.EntityManager);
            activeIds.Dispose();
        }
    }
}