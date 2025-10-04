using BastionFall.Core.Shared.Authoring;
using BastionFall.Core.Shared.Input;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BastionFall.Core.Client.Input
{
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial class ClientReadInputSystem : SystemBase
    {
        
        private EntityQuery _ctxReqQ;
        
        private InputControls _actions;

        // InGame actions
        private InputAction _move2D;
        private InputAction _toggleChat;
        private InputAction _toggleNetStats;

        // Chat actions
        private InputAction _chatSubmit, _chatClose, _chatAutocomplete, _chatHistPrev, _chatHistNext, _chatScroll;

        private bool _chatOpen;

        protected override void OnCreate()
        {
            RequireForUpdate<NetworkStreamInGame>();
            RequireForUpdate<NetcodePlayerInput>();

            _actions = new InputControls();
            _actions.InGameMap.Enable();
            _actions.InChatMap.Disable();

            _ctxReqQ = GetEntityQuery(ComponentType.ReadOnly<InputContextChangeRequest>());
            
            var game = _actions.InGameMap;
            _move2D        = game.Move;
            _toggleChat    = game.ToggleChat;
            _toggleNetStats= game.ToggleNetStats;

            var chat = _actions.InChatMap;
            _chatSubmit      = chat.Submit;
            _chatClose       = chat.CloseChat;
            _chatAutocomplete= chat.Autocomplete;
            _chatHistPrev    = chat.HistoryPrev;
            _chatHistNext    = chat.HistoryNext;
            _chatScroll      = chat.Scroll;
        }

        protected override void OnDestroy() => _actions?.Dispose();

        protected override void OnUpdate()
        {
            // 0) Requests aus UI abholen (fokussiert, geschlossen, …)
            if (!_ctxReqQ.IsEmptyIgnoreFilter)
            {
                using var reqs  = _ctxReqQ.ToComponentDataArray<InputContextChangeRequest>(WorldUpdateAllocator);
                using var ents  = _ctxReqQ.ToEntityArray(WorldUpdateAllocator);

                var newest = reqs[reqs.Length - 1].Next; // letzte gewinnt
                SwitchActionMap(newest);
                SystemAPI.SetSingleton(new InputContextState { Context = newest });
                _chatOpen = newest == InputContext.Chat;

                EntityManager.DestroyEntity(ents);
            }

            var ctx = SystemAPI.GetSingleton<InputContextState>().Context;

            // 1) Lokaler Toggle aus InGame: Chat ein/aus
            if (ctx == InputContext.InGame && _toggleChat.WasPressedThisFrame())
            {
                _chatOpen = !_chatOpen;
                var next = _chatOpen ? InputContext.Chat : InputContext.InGame;
                SwitchActionMap(next);
                SystemAPI.SetSingleton(new InputContextState { Context = next });
                ctx = next;
            }

            // 2) Chat-spezifische Controls: erzeugen wir als ECS-Singleton für die Mono-UI
            EnsureChatUiInputSingleton(out var chatUiEntity);
            var chatInput = SystemAPI.GetComponent<ChatUiInput>(chatUiEntity);
            chatInput = default; // reset per Frame

            if (ctx == InputContext.Chat)
            {
                // Tab → Autocomplete
                if (_chatAutocomplete.WasPressedThisFrame())
                    chatInput.Autocomplete = 1;

                // Up/Down → History
                if (_chatHistPrev.WasPressedThisFrame()) chatInput.HistoryDelta = -1;
                if (_chatHistNext.WasPressedThisFrame()) chatInput.HistoryDelta = +1;

                // Scroll (Mouse Wheel Y)
                var s = _chatScroll.ReadValue<Vector2>().y;
                chatInput.Scroll = s;

                // Submit schließt optional den Chat (UI darf auch offen bleiben)
                if (_chatSubmit.WasPressedThisFrame())
                    chatInput.Submit = 1;

                // Escape → Chat verlassen
                if (_chatClose.WasPressedThisFrame())
                {
                    SwitchActionMap(InputContext.InGame);
                    SystemAPI.SetSingleton(new InputContextState { Context = InputContext.InGame });
                    _chatOpen = false;
                }
            }

            EntityManager.SetComponentData(chatUiEntity, chatInput);

            // 3) Dein bestehender Input → NetcodePlayerInput
            var move = GatherMove(ctx);
            var statsHeld = GatherNetStats(ctx);
            var chatOpen = _chatOpen ? byte.MaxValue : byte.MinValue;

            Entities.WithAll<GhostOwnerIsLocal>().ForEach((ref NetcodePlayerInput input) =>
            {
                input.Move = move;
                input.ChatOpen = chatOpen;
                input.NetStatsHeld = statsHeld;
            }).Run();
        }

        private void SwitchActionMap(InputContext next)
        {
            // genau eine Map aktiv
            if (next == InputContext.InGame)
            {
                _actions.InChatMap.Disable();
                _actions.InGameMap.Enable();
            }
            else if (next == InputContext.Chat)
            {
                _actions.InGameMap.Disable();
                _actions.InChatMap.Enable();
            }
            else // Inventory etc. (später)
            {
                _actions.InGameMap.Disable();
                _actions.InChatMap.Disable();
            }
        }

        private byte GatherNetStats(InputContext ctx)
            => ctx == InputContext.InGame && _toggleNetStats.IsPressed() ? byte.MaxValue : byte.MinValue;

        private Vector2 GatherMove(InputContext ctx)
        {
            if (ctx != InputContext.InGame) return Vector2.zero;
            return _move2D.IsPressed() ? _move2D.ReadValue<Vector2>() : Vector2.zero;
        }

        private void EnsureChatUiInputSingleton(out Entity e)
        {
            if (!SystemAPI.TryGetSingletonEntity<ChatUiInput>(out e))
            {
                e = EntityManager.CreateEntity(typeof(ChatUiInput));
                EntityManager.SetName(e, "ChatUiInputSingleton");
            }
        }
    }
}
