using BastionFall.Core.Shared.Request.Player;
using BastionFall.Features.Player.Shared.Authoring.Appearance;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace BastionFall.Features.Player.Server.Appearance
{
    /// <summary>
    ///     Konsumiert RequestPlayerColorChange und setzt PlayerRenderColor beim Ghost des Absenders.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct ServerApplyRequestedPlayerColorSystem : ISystem
    {
        private EntityQuery _playersQ;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            _playersQ = SystemAPI.QueryBuilder()
                .WithAll<GhostOwner, PlayerRenderColor>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (req, e) in SystemAPI.Query<RefRO<RequestPlayerColorChange>>().WithEntityAccess())
            {
                var wantedId = req.ValueRO.OwnerNetworkId;

                using var ents = _playersQ.ToEntityArray(state.WorldUpdateAllocator);
                using var owners = _playersQ.ToComponentDataArray<GhostOwner>(state.WorldUpdateAllocator);
                for (var i = 0; i < ents.Length; i++)
                {
                    if (owners[i].NetworkId != wantedId) continue;
                    var color = em.GetComponentData<PlayerRenderColor>(ents[i]);
                    color.Rgba = req.ValueRO.Rgba;
                    ecb.SetComponent(ents[i], color);
                    break;
                }

                ecb.DestroyEntity(e);
            }

            ecb.Playback(em);
        }
    }
}