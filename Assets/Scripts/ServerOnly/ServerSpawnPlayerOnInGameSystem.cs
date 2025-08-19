// Assets/Scripts/ServerOnly/ServerSpawnPlayerOnInGameSystem.cs
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Game.Shared.Authoring;
using Game.Shared.Util;

namespace Game.Server
{
    public struct SpawnedPlayer : IComponentData
    {
        public Entity PlayerEntity;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct ServerSpawnPlayerOnInGameSystem : ISystem
    {
        private bool _warnedNoSpawner;

        public void OnCreate(ref SystemState state)
        {
            state.LogInfo("Spawn", "ServerSpawnPlayerOnInGameSystem ONLINE");
        }

        public void OnUpdate(ref SystemState state)
        {
            // Ghosts bereit?
            if (!SystemAPI.HasSingleton<GhostCollection>())
            {
                state.LogInfo("Spawn", "No GhostCollection yet – waiting…");
                return;
            }

            // Spawner vorhanden?
            if (!SystemAPI.TryGetSingleton<PlayerSpawner>(out var spawner))
            {
                if (!_warnedNoSpawner)
                {
                    state.LogError("Spawn", "No PlayerSpawner singleton in world – cannot spawn players!");
                    _warnedNoSpawner = true;
                }
                return;
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Für jede InGame-Connection ohne bereits gespawnten Player → Player instantiieren
            foreach (var (netId, connEntity) in SystemAPI.Query<RefRO<NetworkId>>()
                         .WithAll<NetworkStreamInGame>()
                         .WithNone<SpawnedPlayer>()
                         .WithEntityAccess())
            {
                var player = state.EntityManager.Instantiate(spawner.PlayerPrefab);
                state.EntityManager.SetComponentData(player, new GhostOwner { NetworkId = netId.ValueRO.Value });

                ecb.AddComponent(connEntity, new SpawnedPlayer { PlayerEntity = player });
                state.LogInfo("Spawn", "Spawned player ghost for netId={0}", netId.ValueRO.Value);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
