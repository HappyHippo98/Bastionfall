using BastionFall.Core.Shared.Request.Player;
using BastionFall.Features.Chat.Shared.Commands;
using BastionFall.Features.Chat.Shared.RPC;
using BastionFall.Features.Player.Shared.Authoring.Monitoring;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace BastionFall.Features.Chat.Server
{
    /// <summary>
    ///     Server empfängt ChatSendRpc, verarbeitet Commands und broadcastet ChatMessageRpc.
    ///     Feature-Entkopplung: Für Player-Funktionen erzeugen wir nur Requests (Core.Shared).
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct ServerHandleChatRpcSystem : ISystem
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

            foreach (var (send, req, rpcEnt) in SystemAPI
                         .Query<RefRO<ChatSendRpc>, RefRO<ReceiveRpcCommandRequest>>()
                         .WithEntityAccess())
            {
                var srcConn = req.ValueRO.SourceConnection;
                var senderId = em.HasComponent<NetworkId>(srcConn) ? em.GetComponentData<NetworkId>(srcConn).Value : -1;
                var senderName = (FixedString64Bytes)"Player";

                // Name aus PlayerIdentity an der Connection holen, falls schon gesetzt
                if (em.HasComponent<PlayerIdentity>(srcConn))
                    senderName = em.GetComponentData<PlayerIdentity>(srcConn).DisplayName;

                var raw = send.ValueRO.Text.ToString();
                var text = raw.Trim();

                if (!string.IsNullOrEmpty(text))
                {
                    var startsWithSlash = text[0] == '/';

                    if (ChatCommandParser.TryParse(text, out var type, out var arg))
                        HandleCommand(ref state, type, arg, senderId, senderName, ecb);
                    else if (startsWithSlash)
                        BroadcastSystemToAll(ref state, (FixedString512Bytes)$"{senderName} unknown command '{text}'",
                            ecb);
                    else
                        BroadcastChat(ref state, senderId, senderName, send.ValueRO.Text, ecb);
                }

                ecb.DestroyEntity(rpcEnt);
            }

            ecb.Playback(em);
        }

        private void BroadcastChat(ref SystemState state, int senderId, FixedString64Bytes senderName,
            FixedString512Bytes text, EntityCommandBuffer ecb)
        {
            using var conns = _inGameConnsQ.ToEntityArray(state.WorldUpdateAllocator);
            for (var i = 0; i < conns.Length; i++)
            {
                var e = ecb.CreateEntity();
                ecb.AddComponent(e, new ChatMessageRpc
                {
                    SenderNetworkId = senderId,
                    SenderName = senderName,
                    Text = text
                });
                ecb.AddComponent(e, new SendRpcCommandRequest { TargetConnection = conns[i] });
            }
        }

        private void BroadcastSystemToAll(ref SystemState state, FixedString512Bytes text, EntityCommandBuffer ecb)
        {
            using var conns = _inGameConnsQ.ToEntityArray(state.WorldUpdateAllocator);
            for (var i = 0; i < conns.Length; i++)
            {
                var e = ecb.CreateEntity();
                ecb.AddComponent(e, new ChatMessageRpc
                {
                    SenderNetworkId = 0,
                    SenderName = (FixedString64Bytes)"SERVER",
                    Text = text
                });
                ecb.AddComponent(e, new SendRpcCommandRequest { TargetConnection = conns[i] });
            }
        }

        private void HandleCommand(ref SystemState state, ChatCommandType type, string arg, int senderId,
            FixedString64Bytes senderName, EntityCommandBuffer ecb)
        {
            switch (type)
            {
                case ChatCommandType.ChangeColor:
                {
                    if (!ColorUtil.TryParseColor(arg ?? "", out var rgba))
                    {
                        BroadcastSystemToAll(ref state,
                            (FixedString512Bytes)
                            $"{senderName} /changecolor – Syntax: /changecolor <name|#RRGGBB|r,g,b>", ecb);
                        break;
                    }

                    // Request an Player-Feature (entkoppelt)
                    var req = ecb.CreateEntity();
                    ecb.AddComponent(req, new RequestPlayerColorChange { OwnerNetworkId = senderId, Rgba = rgba });

                    // Server-Meldung mit Namen & Wunschfarbe
                    BroadcastSystemToAll(ref state,
                        (FixedString512Bytes)$"player {senderName} changed color to {arg}", ecb);

                    // (kein normales Chat-echo mehr)
                    break;
                }
            }
        }
    }
}
