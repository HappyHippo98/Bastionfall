using Shared.Authoring.Monitoring; // PlayerIdentity (am Ghost für Namen)
using Shared.Rpc;                  // ChatMessageRpc
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ServerOnly.Systems
{
    /// <summary>
    /// Findet Player-Ghosts, deren Owner-NetworkId keine aktive InGame-Connection mehr hat.
    /// -> broadcastet "<Name> despawned" (SERVER) und zerstört den Ghost.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ServerCopyIdentityToGhostSystem))]
    [BurstCompile]
    public partial struct ServerDespawnPlayersOnDisconnectSystem : ISystem
    {
        private EntityQuery _inGameConnsQ;
        private EntityQuery _playersQ;

        public void OnCreate(ref SystemState s)
        {
            s.RequireForUpdate<NetworkStreamInGame>();

            _inGameConnsQ = SystemAPI.QueryBuilder()
                .WithAll<NetworkId, NetworkStreamInGame, NetworkStreamConnection>()
                .Build();

            _playersQ = SystemAPI.QueryBuilder()
                .WithAll<GhostOwner, PlayerIdentity>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState s)
        {
            if (_playersQ.IsEmptyIgnoreFilter) return;

            var em  = s.EntityManager;
            var ecb = new EntityCommandBuffer(s.WorldUpdateAllocator);

            // Aktive Owner-Ids einsammeln
            using var connIds = _inGameConnsQ.ToComponentDataArray<NetworkId>(s.WorldUpdateAllocator);
            var activeOwners = new NativeParallelHashSet<int>(connIds.Length, s.WorldUpdateAllocator);
            for (int i = 0; i < connIds.Length; i++) activeOwners.Add(connIds[i].Value);

            // Alle Player-Ghosts prüfen
            using var ents   = _playersQ.ToEntityArray(s.WorldUpdateAllocator);
            using var owners = _playersQ.ToComponentDataArray<GhostOwner>(s.WorldUpdateAllocator);
            using var names  = _playersQ.ToComponentDataArray<PlayerIdentity>(s.WorldUpdateAllocator);

            for (int i = 0; i < ents.Length; i++)
            {
                int ownerId = owners[i].NetworkId;
                if (activeOwners.Contains(ownerId)) continue; // owner noch verbunden

                // Broadcast "<Name> despawned" an alle
                using var targets = _inGameConnsQ.ToEntityArray(s.WorldUpdateAllocator);
                for (int t = 0; t < targets.Length; t++)
                {
                    var rpc = ecb.CreateEntity();
                    ecb.AddComponent(rpc, new ChatMessageRpc
                    {
                        SenderNetworkId = 0,
                        SenderName      = (FixedString64Bytes)"SERVER",
                        Text            = (FixedString512Bytes)$"{names[i].DisplayName} despawned"
                    });
                    ecb.AddComponent(rpc, new SendRpcCommandRequest { TargetConnection = targets[t] });
                }

                // Ghost loswerden
                ecb.DestroyEntity(ents[i]);
            }

            ecb.Playback(em);
        }
    }
}
