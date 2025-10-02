﻿using Shared.Authoring.Appearance;   // PlayerRenderColor
using Shared.Authoring.Monitoring;   // PlayerIdentity (Name/Guid)
using Shared.Chat;
using Shared.Rpc;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ServerOnly.Systems
{
    /// <summary>
    /// Server empfängt ChatSendRpc, verarbeitet ggf. Commands und broadcastet ChatMessageRpc.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(RpcSystem))]
    [BurstCompile]
    public partial struct ServerHandleChatRpcSystem : ISystem
    {
        private EntityQuery _inGameConnsQ;
        private EntityQuery _playersWithColorQ;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();

            _inGameConnsQ = SystemAPI.QueryBuilder()
                .WithAll<NetworkId, NetworkStreamInGame, NetworkStreamConnection>()
                .Build();

            _playersWithColorQ = SystemAPI.QueryBuilder()
                .WithAll<GhostOwner, PlayerRenderColor>()
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
                var srcConn    = req.ValueRO.SourceConnection;
                var senderId   = em.HasComponent<NetworkId>(srcConn) ? em.GetComponentData<NetworkId>(srcConn).Value : -1;
                var senderName = em.HasComponent<PlayerIdentity>(srcConn)
                                 ? em.GetComponentData<PlayerIdentity>(srcConn).DisplayName
                                 : new FixedString64Bytes("Player");

                var raw = send.ValueRO.Text.ToString();
                var text = raw.Trim();

                if (!string.IsNullOrEmpty(text))
                {
                    var startsWithSlash = text.Length > 0 && text[0] == '/';

                    if (ChatCommandParser.TryParse(text, out var type, out var arg))
                    {
                        HandleCommand(ref state, type, arg, senderId, senderName, ecb);
                    }
                    else if (startsWithSlash)
                    {
                        // Unbekannter Command → ServerMsg an alle
                        BroadcastSystemToAll(ref state, (FixedString512Bytes)$"{senderName} unknown command '{text}'", ecb);
                    }
                    else
                    {
                        // normale Chat-Nachricht an alle
                        BroadcastChat(ref state, senderId, senderName, send.ValueRO.Text, ecb);
                    }
                }

                ecb.DestroyEntity(rpcEnt);
            }

            ecb.Playback(em);
        }

        private void BroadcastChat(ref SystemState state, int senderId, FixedString64Bytes senderName, FixedString512Bytes text, EntityCommandBuffer ecb)
        {
            using var conns = _inGameConnsQ.ToEntityArray(state.WorldUpdateAllocator);
            for (int i = 0; i < conns.Length; i++)
            {
                var e = ecb.CreateEntity();
                ecb.AddComponent(e, new ChatMessageRpc
                {
                    SenderNetworkId = senderId,
                    SenderName      = senderName,
                    Text            = text
                });
                ecb.AddComponent(e, new SendRpcCommandRequest { TargetConnection = conns[i] });
            }
        }

        private void BroadcastSystemToAll(ref SystemState state, FixedString512Bytes text, EntityCommandBuffer ecb)
        {
            using var conns = _inGameConnsQ.ToEntityArray(state.WorldUpdateAllocator);
            for (int i = 0; i < conns.Length; i++)
            {
                var e = ecb.CreateEntity();
                ecb.AddComponent(e, new ChatMessageRpc
                {
                    SenderNetworkId = 0,
                    SenderName      = (FixedString64Bytes)"SERVER",
                    Text            = text
                });
                ecb.AddComponent(e, new SendRpcCommandRequest { TargetConnection = conns[i] });
            }
        }

        private void HandleCommand(ref SystemState state, ChatCommandType type, string arg, int senderId, FixedString64Bytes senderName, EntityCommandBuffer ecb)
        {
            var em = state.EntityManager;

            switch (type)
            {
                case ChatCommandType.ChangeColor:
                {
                    if (!ColorUtil.TryParseColor(arg ?? "", out var rgba))
                    {
                        // Syntax-Hinweis an alle
                        BroadcastSystemToAll(ref state,
                            (FixedString512Bytes)$"{senderName} /changecolor aufgerufen – Syntax: /changecolor <name|#RRGGBB|r,g,b>",
                            ecb);
                        return;
                    }

                    // passenden Player-Ghost (Besitzer == senderId) finden
                    using var ents   = _playersWithColorQ.ToEntityArray(state.WorldUpdateAllocator);
                    using var owners = _playersWithColorQ.ToComponentDataArray<GhostOwner>(state.WorldUpdateAllocator);

                    Entity target = Entity.Null;
                    for (int i = 0; i < ents.Length; i++)
                        if (owners[i].NetworkId == senderId) { target = ents[i]; break; }

                    if (target == Entity.Null)
                    {
                        BroadcastSystemToAll(ref state,
                            (FixedString512Bytes)$"{senderName} color change failed (no player ghost found).",
                            ecb);
                        return;
                    }

                    var c = em.GetComponentData<PlayerRenderColor>(target);
                    c.Rgba = rgba;
                    em.SetComponentData(target, c);

                    // Feedback an alle:
                    BroadcastChat(ref state, senderId, senderName, (FixedString512Bytes)"*changed color*", ecb);
                    break;
                }
            }
        }
    }
}
