using BastionFall.Features.Chat.Shared.Authoring;
using BastionFall.Features.Chat.Shared.RPC;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

// ChatBuffer, ChatMessageEntry
// ChatMessageRpc

namespace BastionFall.Features.Chat.Client.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(RpcSystem))]
    [BurstCompile]
    public partial class ClientReceiveChatSystem : SystemBase
    {
        private EntityQuery _chatQ;

        protected override void OnCreate()
        {
            RequireForUpdate<NetworkStreamInGame>();
            _chatQ = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<ChatBuffer>(), // Konfig-Komponente mit MaxMessages
                    ComponentType.ReadOnly<ChatMessageEntry>() // Wir wollen nur Entities, die den Buffer tragen
                }
            });
        }

        [BurstCompile]
        protected override void OnUpdate()
        {
            if (_chatQ.IsEmptyIgnoreFilter) return;

            var em = EntityManager;
            var ecb = new EntityCommandBuffer(WorldUpdateAllocator);

            // Wir nehmen das erste Chat-Entity (du hast typischerweise genau eins)
            using var chatEntities = _chatQ.ToEntityArray(WorldUpdateAllocator);
            var chatEntity = chatEntities[0];

            var cfg = em.GetComponentData<ChatBuffer>(chatEntity);
            var buf = em.GetBuffer<ChatMessageEntry>(chatEntity);

            foreach (var (rpc, req, rpcEnt) in SystemAPI
                         .Query<RefRO<ChatMessageRpc>, RefRO<ReceiveRpcCommandRequest>>()
                         .WithEntityAccess())
            {
                buf.Add(new ChatMessageEntry
                {
                    SenderNetworkId = rpc.ValueRO.SenderNetworkId,
                    SenderName = rpc.ValueRO.SenderName,
                    Text = rpc.ValueRO.Text,
                    ServerTick = 0
                });

                var over = buf.Length - cfg.MaxMessages;
                if (over > 0) buf.RemoveRange(0, over);

                ecb.DestroyEntity(rpcEnt);
            }

            ecb.Playback(em);
        }
    }
}