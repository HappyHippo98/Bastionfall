using BastionFall.Core.Shared.Request.Server;
using BastionFall.Features.Player.Shared.Authoring.Monitoring;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace BastionFall.Features.Player.Server.Lifecycle
{
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

            var em = s.EntityManager;
            var ecb = new EntityCommandBuffer(s.WorldUpdateAllocator);

            using var connIds = _inGameConnsQ.ToComponentDataArray<NetworkId>(s.WorldUpdateAllocator);
            var activeOwners = new NativeParallelHashSet<int>(connIds.Length, s.WorldUpdateAllocator);
            for (var i = 0; i < connIds.Length; i++) activeOwners.Add(connIds[i].Value);

            using var ents = _playersQ.ToEntityArray(s.WorldUpdateAllocator);
            using var owners = _playersQ.ToComponentDataArray<GhostOwner>(s.WorldUpdateAllocator);
            using var names = _playersQ.ToComponentDataArray<PlayerIdentity>(s.WorldUpdateAllocator);

            for (var i = 0; i < ents.Length; i++)
            {
                var ownerId = owners[i].NetworkId;
                if (activeOwners.Contains(ownerId)) continue;

                // System-Message request (statt Chat RPC)
                var msgE = ecb.CreateEntity();
                ecb.AddComponent(msgE, new RequestBroadcastSystemMessage
                {
                    Text = (FixedString512Bytes)$"{names[i].DisplayName} despawned"
                });

                ecb.DestroyEntity(ents[i]);
            }

            ecb.Playback(em);
        }
    }
}