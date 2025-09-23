using Shared.Authoring.Monitoring;
using Shared.Authoring.Network;
using Shared.Rpc;
using Unity.Entities;
using Unity.NetCode;

namespace ServerOnly
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ServerHandleIdentifyPlayerRpcSystem : ISystem
    {
        private EntityQuery _playersQ;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableNetcode>();
            _playersQ = state.GetEntityQuery(
                ComponentType.ReadOnly<PlayerIdentity>(),
                ComponentType.ReadOnly<GhostOwner>());
        }

        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            if (SystemAPI.QueryBuilder().WithAll<IdentifyPlayerRpc, ReceiveRpcCommandRequest>().Build()
                .IsEmptyIgnoreFilter)
                return;

            using var owners = _playersQ.ToComponentDataArray<GhostOwner>(state.WorldUpdateAllocator);
            using var ents = _playersQ.ToEntityArray(state.WorldUpdateAllocator);

            foreach (var (rpc, req, rpcEnt) in SystemAPI
                         .Query<RefRO<IdentifyPlayerRpc>, RefRO<ReceiveRpcCommandRequest>>()
                         .WithEntityAccess())
            {
                // Owner immer aus SourceConnection bestimmen (wie bei NetStats). :contentReference[oaicite:1]{index=1}
                var ownerId = -1;
                if (em.HasComponent<NetworkId>(req.ValueRO.SourceConnection))
                    ownerId = em.GetComponentData<NetworkId>(req.ValueRO.SourceConnection).Value;

                if (ownerId != -1)
                    for (var i = 0; i < ents.Length; i++)
                    {
                        if (owners[i].NetworkId != ownerId) continue;
                        var id = em.GetComponentData<PlayerIdentity>(ents[i]);
                        id.Guid = rpc.ValueRO.Guid;
                        id.Name = rpc.ValueRO.Name;
                        em.SetComponentData(ents[i], id);
                        break;
                    }

                ecb.DestroyEntity(rpcEnt);
            }

            ecb.Playback(em);
        }
    }
}