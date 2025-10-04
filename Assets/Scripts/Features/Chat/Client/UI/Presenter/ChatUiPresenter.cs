using BastionFall.Core.Shared.Authoring;
using BastionFall.Core.Shared.Input;
using BastionFall.Features.Chat.Client.Systems;
using BastionFall.Features.Chat.Shared.Authoring;
using BastionFall.Features.Chat.Shared.Constants;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;

namespace BastionFall.Features.Chat.Client.UI.Presenter
{
    public class ChatUiPresenter : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private ChatUI chatUI;
        [SerializeField] private ChatFeedUI chatFeedUI;

        [Tooltip("Optional: wird benutzt für Scroll/Auto-Scroll. Wenn leer, versucht der Presenter es am Chat-Panel zu finden.")]
        [SerializeField] private ScrollRect scrollRect;

        [Tooltip("Optional: Inputfield für Fokus/History/Autocomplete-Fallbacks.")]
        [SerializeField] private TMP_InputField inputField;

        private EntityManager _em;
        private EntityQuery _localInputQ;
        private EntityQuery _chatQ;
        private EntityQuery _chatUiInputQ;

        private int _lastSeen;
        private const float BottomThreshold = 0.02f; // 0 = ganz unten, 1 = ganz oben
        private const float ScrollSpeed = 0.08f;

        #region Unity

        private void Awake()
        {
            if (!chatUI)     chatUI     = FindObjectOfType<ChatUI>(true);
            if (!chatFeedUI) chatFeedUI = FindObjectOfType<ChatFeedUI>(true);
            if (!scrollRect && chatFeedUI) scrollRect = chatFeedUI.GetComponentInChildren<ScrollRect>(true);
            if (!inputField && chatUI)     inputField = chatUI.GetComponentInChildren<TMP_InputField>(true);
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

            _chatUiInputQ = _em.CreateEntityQuery(ComponentType.ReadOnly<ChatUiInput>());

            _lastSeen = 0;

            // UI-Fokus-Events -> Context setzen (bewirkt Map-Wechsel im ClientReadInputSystem)
            if (inputField)
            {
                inputField.onSelect.AddListener(_ =>  SetContextIfDifferent(InputContext.Chat));
                inputField.onDeselect.AddListener(_ =>  SetContextIfDifferent(InputContext.InGame));
                //inputField.onSubmit.AddListener(_ =>  SetContextIfDifferent(InputContext.InGame));
            }
        }
        
        

        private void LateUpdate()
        {
            if (_em == default) return;

            HandleOpenCloseFromNetcodeInput();
            FlushChatBufferWithAutoScroll();
            ConsumeChatUiInputSingleton();
        }

        #endregion

        #region Handlers

        private void HandleOpenCloseFromNetcodeInput()
        {
            if (_localInputQ.CalculateEntityCount() == 0 || chatUI == null) return;

            using var ents = _localInputQ.ToEntityArray(Allocator.Temp);
            var e     = ents[0];
            var input = _em.GetComponentData<NetcodePlayerInput>(e);

            // FIX: ChatOpen ist ein byte-Flag -> NICHT mit "== 1" prüfen!
            bool wantOpen = input.ChatOpen != 0;

            if (wantOpen && !chatUI.IsTyping)
            {
                chatUI.Open();
                SetContextIfDifferent(InputContext.Chat);
                FocusInput();
            }
            else if (!wantOpen && chatUI.IsTyping)
            {
                chatUI.Close();
                SetContextIfDifferent(InputContext.InGame);
            }
        }

        private void FlushChatBufferWithAutoScroll()
        {
            if (chatFeedUI == null || _chatQ.IsEmptyIgnoreFilter) return;

            using var ents = _chatQ.ToEntityArray(Allocator.Temp);
            if (ents.Length == 0) return;

            var chatEntity = ents[0];
            var buf = _em.GetBuffer<ChatMessageEntry>(chatEntity);

            // Wenn Buffer zurückgesetzt wurde (z. B. bei Rejoin)
            if (buf.Length < _lastSeen) _lastSeen = 0;

            // Auto-Scroll nur, wenn der User am Ende ist
            bool wasAtBottom = IsUserAtBottom();

            for (var i = _lastSeen; i < buf.Length; i++)
            {
                var m = buf[i];
                var type = Classify(m.SenderName.ToString(), m.Text.ToString());
                chatFeedUI.AddItem(m.SenderName.ToString(), m.Text.ToString(), type);
            }
            _lastSeen = buf.Length;

            if (wasAtBottom) ScrollToBottom();
        }

        private void ConsumeChatUiInputSingleton()
        {
            if (_chatUiInputQ.IsEmptyIgnoreFilter) return;

            var e = _chatUiInputQ.GetSingletonEntity();
            var data = _em.GetComponentData<ChatUiInput>(e);

            // Scrollen (nur wenn UI offen, sonst ignorieren)
            if (Mathf.Abs(data.Scroll) > 0.0001f && chatUI != null && chatUI.IsTyping)
                AddScroll(data.Scroll * ScrollSpeed);

            // History
            if (data.HistoryDelta != 0 && chatUI != null && chatUI.IsTyping)
                ApplyHistoryDelta(data.HistoryDelta);

            // Autocomplete
            if (data.Autocomplete == 1 && chatUI != null && chatUI.IsTyping)
                Autocomplete();

            // Submit (nur falls du zusätzlich zum onSubmit ein „Enter“ aus der Map willst)
            if (data.Submit == 1 && chatUI != null && chatUI.IsTyping)
                SubmitCurrentMessage();
        }

        #endregion

        #region UI helpers

        private void FocusInput()
        {
            if (!inputField) return;
            inputField.ActivateInputField();
            inputField.MoveTextEnd(false);
        }

        private bool IsUserAtBottom()
        {
            if (!scrollRect) return true; // ohne ScrollRect: immer auto-scroll
            return scrollRect.verticalNormalizedPosition <= BottomThreshold;
        }

        private void ScrollToBottom()
        {
            if (!scrollRect) return;
            // 0 = unten, 1 = oben
            scrollRect.verticalNormalizedPosition = 0f;
            // ForceUpdate, falls LayoutGroups verwenden
            Canvas.ForceUpdateCanvases();
        }

        private void AddScroll(float deltaNorm)
        {
            if (!scrollRect) return;
            scrollRect.verticalNormalizedPosition =
                Mathf.Clamp01(scrollRect.verticalNormalizedPosition + deltaNorm);
        }

        private void ApplyHistoryDelta(int delta)
        {
            if (!inputField) return;

            var curr = inputField.text;
            var next = ChatClientLocalHistory.Step(delta, curr);

            // Text setzen ohne Events zu spammen
            inputField.SetTextWithoutNotify(next ?? string.Empty);
            inputField.caretPosition = next?.Length ?? 0;
            inputField.selectionAnchorPosition = inputField.caretPosition;
            inputField.selectionFocusPosition  = inputField.caretPosition;
        }


        private void Autocomplete()
        {
            // Hook für Autocomplete – typischerweise:
            // var prefix = inputField.text;
            // var suggestion = ChatClientApi.Instance?.Suggest(prefix);
            // if (!string.IsNullOrEmpty(suggestion)) inputField.text = suggestion;
        }

        private void SubmitCurrentMessage()
        {
            // Falls du onSubmit schon nutzt, reicht Focus-Verhalten.
            // Optional: hier explizit auslösen:
            // chatUI.TrySendAndClear();
        }

        private static MessageType Classify(string sender, string text)
        {
            if (!string.IsNullOrEmpty(sender) && sender.ToUpperInvariant() is "SERVER" or "SYSTEM")
                return MessageType.ServerMsg;
            if (!string.IsNullOrEmpty(text) && text.Length > 0 && text[0] == '/')
                return MessageType.Command;
            return MessageType.Info;
        }

        private void SetContextIfDifferent(InputContext wanted)
        {
            if (_em == default) return;
            var current = _em.CreateEntityQuery(ComponentType.ReadOnly<InputContextState>())
                .GetSingleton<InputContextState>().Context;
            if (current != wanted)
                InputContextApi.Set(wanted);
        }

        #endregion
    }
}
