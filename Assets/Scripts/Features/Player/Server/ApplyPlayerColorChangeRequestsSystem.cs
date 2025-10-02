using BastionFall.Core.Shared.Request.Player;
using BastionFall.Features.Player.Shared.Authoring.Appearance;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

// PlayerRenderColor

namespace BastionFall.Features.Player.Server
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct ApplyPlayerColorChangeRequestsSystem : ISystem
    {
        private EntityQuery _playersQ;
        private EntityQuery _reqQ;

        public void OnCreate(ref SystemState s)
        {
            _playersQ = SystemAPI.QueryBuilder()
                .WithAll<GhostOwner, PlayerRenderColor>()
                .Build();

            _reqQ = SystemAPI.QueryBuilder()
                .WithAll<RequestPlayerColorChange>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState s)
        {
            if (_reqQ.IsEmptyIgnoreFilter) return;

            var em = s.EntityManager;
            var ecb = new EntityCommandBuffer(s.WorldUpdateAllocator);

            using var ents = _playersQ.ToEntityArray(s.WorldUpdateAllocator);
            using var owners = _playersQ.ToComponentDataArray<GhostOwner>(s.WorldUpdateAllocator);

            foreach (var (req, reqEnt) in SystemAPI
                         .Query<RefRO<RequestPlayerColorChange>>()
                         .WithEntityAccess())
            {
                var ownerId = req.ValueRO.OwnerNetworkId;

                // passenden Player-Ghost suchen
                for (var i = 0; i < ents.Length; i++)
                {
                    if (owners[i].NetworkId != ownerId) continue;
                    var c = em.GetComponentData<PlayerRenderColor>(ents[i]);
                    c.Rgba = req.ValueRO.Rgba;
                    em.SetComponentData(ents[i], c);
                    break;
                }

                ecb.DestroyEntity(reqEnt);
            }

            ecb.Playback(em);
        }
    }
}