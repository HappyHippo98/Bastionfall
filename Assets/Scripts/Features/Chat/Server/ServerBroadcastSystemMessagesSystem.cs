using BastionFall.Core.Shared.Request.Server;
using BastionFall.Features.Chat.Shared.RPC;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace BastionFall.Features.Chat.Server
{
    /// <summary>
    ///     Verarbeitet RequestBroadcastSystemMessage (Core.Shared) und sendet ChatMessageRpc an alle InGame-Connections.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(RpcSystem))]
    [BurstCompile]
    public partial struct ServerBroadcastSystemMessagesSystem : ISystem
    {
        private EntityQuery _inGameConnsQ;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            _inGameConnsQ = SystemAPI.QueryBuilder()
                .WithAll<NetworkId, NetworkStreamInGame, NetworkStreamConnection>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (msg, e) in SystemAPI.Query<RefRO<RequestBroadcastSystemMessage>>().WithEntityAccess())
            {
                using var conns = _inGameConnsQ.ToEntityArray(state.WorldUpdateAllocator);
                for (var i = 0; i < conns.Length; i++)
                {
                    var rpc = ecb.CreateEntity();
                    ecb.AddComponent(rpc, new ChatMessageRpc
                    {
                        SenderNetworkId = 0,
                        SenderName = (FixedString64Bytes)"SERVER",
                        Text = msg.ValueRO.Text
                    });
                    ecb.AddComponent(rpc, new SendRpcCommandRequest { TargetConnection = conns[i] });
                }

                ecb.DestroyEntity(e);
            }

            ecb.Playback(em);
        }
    }
}