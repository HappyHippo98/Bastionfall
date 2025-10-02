﻿using ClientOnly.Authoring;       
using Shared.Authoring.Chat;
using Shared.UI; 
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace ClientOnly.Monobehaviours
{

    public class UiEcsBridge : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private ChatUI chatUI;
        [SerializeField] private ChatFeedUI chatFeedUI;
        [SerializeField] private PlayerNetStatDataUI netStatsUI;

        private World _world;
        private EntityManager _em;

        private EntityQuery _localInputQ;
        private EntityQuery _chatQ;

        private bool _initialized;
        private int _lastSeenChatCount;

        private bool EnsureReady()
        {
            if (_initialized) return true;

            _world = ClientServerBootstrap.ClientWorld;
            if (_world == null) return false;

            _em = _world.EntityManager;

            _localInputQ = _em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<NetcodePlayerInput>(),
                    ComponentType.ReadOnly<GhostOwnerIsLocal>()
                }
            });

            _chatQ = _em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<ChatBuffer>(),
                    ComponentType.ReadOnly<ChatMessageEntry>()
                }
            });

            _lastSeenChatCount = 0;
            _initialized = true;
            return true;
        }

        private void LateUpdate()
        {
            if (!EnsureReady()) return;

            ApplyHotkeys();
            FlushNewChatEntries();
        }

        private void ApplyHotkeys()
        {
            if (_localInputQ.CalculateEntityCount() == 0) return;

            using var ents = _localInputQ.ToEntityArray(Allocator.Temp);
            if (ents.Length == 0) return;

            var e = ents[0];
            var input = _em.GetComponentData<NetcodePlayerInput>(e);

            if (chatUI != null)
            {
                if (input.ChatOpen == 1 && !chatUI.IsTyping)
                    chatUI.Open();
                else if (input.ChatOpen == 0 && chatUI.IsTyping)
                    chatUI.Close();
            }

            if (netStatsUI != null)
                netStatsUI.SetVisible(input.NetStatsHeld == 1);
        }

        private void FlushNewChatEntries()
        {
            if (chatFeedUI == null) return;
            if (_chatQ.IsEmptyIgnoreFilter) return;

            using var ents = _chatQ.ToEntityArray(Allocator.Temp);
            if (ents.Length == 0) return;

            var chatEntity = ents[0];
            var buf = _em.GetBuffer<ChatMessageEntry>(chatEntity);

            if (buf.Length < _lastSeenChatCount) _lastSeenChatCount = 0;

            for (int i = _lastSeenChatCount; i < buf.Length; i++)
            {
                var m = buf[i];

                // Deterministische Klassifikation (kein Random)
                var type = Classify(m.SenderName.ToString(), m.Text.ToString());

                chatFeedUI.AddItem(
                    m.SenderName.ToString(),
                    m.Text.ToString(),
                    type
                );
            }

            _lastSeenChatCount = buf.Length;

            if (chatUI != null)
                chatFeedUI.SetTyping(chatUI.IsTyping);
        }

        private static MessageType Classify(string sender, string text)
        {
            if (!string.IsNullOrEmpty(sender) && sender.ToUpperInvariant() is "SERVER" or "SYSTEM")
                return MessageType.ServerMsg;

            if (!string.IsNullOrEmpty(text) && text.Length > 0 && text[0] == '/')
                return MessageType.Command;

            return MessageType.Info;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            if (!chatUI)     chatUI = FindObjectOfType<ChatUI>(true);
            if (!chatFeedUI) chatFeedUI = FindObjectOfType<ChatFeedUI>(true);
            if (!netStatsUI) netStatsUI = FindObjectOfType<PlayerNetStatDataUI>(true);
        }
#endif
    }
}
