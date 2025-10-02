using BastionFall.Core.Shared.Input;
using BastionFall.Features.Chat.Shared.Authoring;
using BastionFall.Features.Chat.Shared.Constants;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace BastionFall.Features.Chat.Client.UI.Presenter
{
    public class ChatUiPresenter : MonoBehaviour
    {
        [SerializeField] private ChatUI chatUI;
        [SerializeField] private ChatFeedUI chatFeedUI;
        private EntityQuery _chatQ;

        private EntityManager _em;
        private int _lastSeen;
        private EntityQuery _localInputQ;

        private void Awake()
        {
            if (!chatUI) chatUI = FindObjectOfType<ChatUI>(true);
            if (!chatFeedUI) chatFeedUI = FindObjectOfType<ChatFeedUI>(true);
        }

        private void Start()
        {
            var world = ClientServerBootstrap.ClientWorld;
            if (world == null) return;

            _em = world.EntityManager;

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

            _lastSeen = 0;
        }

        private void LateUpdate()
        {
            if (_em == default) return;

            // Hotkey → UI öffnen/schließen
            if (_localInputQ.CalculateEntityCount() > 0)
            {
                using var ents = _localInputQ.ToEntityArray(Allocator.Temp);
                var e = ents[0];
                var input = _em.GetComponentData<NetcodePlayerInput>(e);

                if (chatUI != null)
                {
                    if (input.ChatOpen == 1 && !chatUI.IsTyping) chatUI.Open();
                    else if (input.ChatOpen == 0 && chatUI.IsTyping) chatUI.Close();
                }
            }

            // neue Chat-Einträge flushen
            if (chatFeedUI != null && !_chatQ.IsEmptyIgnoreFilter)
            {
                using var ents = _chatQ.ToEntityArray(Allocator.Temp);
                if (ents.Length == 0) return;

                var chatEntity = ents[0];
                var buf = _em.GetBuffer<ChatMessageEntry>(chatEntity);

                if (buf.Length < _lastSeen) _lastSeen = 0;

                for (var i = _lastSeen; i < buf.Length; i++)
                {
                    var m = buf[i];
                    var type = Classify(m.SenderName.ToString(), m.Text.ToString());
                    chatFeedUI.AddItem(m.SenderName.ToString(), m.Text.ToString(), type);
                }

                _lastSeen = buf.Length;
            }
        }

        private static MessageType Classify(string sender, string text)
        {
            if (!string.IsNullOrEmpty(sender) && sender.ToUpperInvariant() is "SERVER" or "SYSTEM")
                return MessageType.ServerMsg;
            if (!string.IsNullOrEmpty(text) && text.Length > 0 && text[0] == '/')
                return MessageType.Command;
            return MessageType.Info;
        }
    }
}