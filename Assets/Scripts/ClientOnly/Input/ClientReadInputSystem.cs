using ClientOnly.Authoring;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine.InputSystem;

namespace ClientOnly.Input
{
    /// <summary>
    /// Liest das neue Input System (InputActions) und schreibt in NetcodePlayerInput.
    /// - Move: Vector2 (WASD)
    /// - ToggleChat: flippt lokalen Zustand _chatOpen (0/1) -> in Komponente als ChatOpen
    /// - ToggleNetStats: halten -> NetStatsHeld
    /// Läuft in der GhostInputSystemGroup.
    /// </summary>
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial class ClientReadInputSystem : SystemBase
    {
        private InputControls _actions;
        private InputAction _move2D;
        private InputAction _toggleChat;
        private InputAction _toggleNetStats;

        // Lokaler Zustand für Chat-Toggle
        private bool _chatOpen;

        protected override void OnCreate()
        {
            RequireForUpdate<NetworkStreamInGame>();
            RequireForUpdate<NetcodePlayerInput>();

            _actions = new InputControls();
            _actions.Enable();

            var map = _actions.InGameMap;
            _move2D         = map.Move;            // Vector2 (WASD 2D Composite)
            _toggleChat     = map.ToggleChat;      // Button (z.B. T oder Enter)
            _toggleNetStats = map.ToggleNetStats;  // Button (z.B. Tab, halten)
        }

        protected override void OnDestroy()
        {
            _actions?.Dispose();
        }

        protected override void OnUpdate()
        {
            // 2D Move lesen
            var v = _move2D.IsPressed() ? _move2D.ReadValue<UnityEngine.Vector2>() : UnityEngine.Vector2.zero;
            float2 move = new float2(v.x, v.y);

            // Chat Toggle -> flippe lokalen Zustand wenn Taste in diesem Frame gedrückt wurde
            if (_toggleChat.WasPressedThisFrame())
                _chatOpen = !_chatOpen;

            byte chatOpen  = (byte)(_chatOpen ? 1 : 0);
            byte statsHeld = (byte)(_toggleNetStats.IsPressed() ? 1 : 0);

            // .Run() -> Werte stehen im selben Frame bereit (vor Mono.Update)
            Entities
                .WithAll<GhostOwnerIsLocal>()
                .ForEach((ref NetcodePlayerInput input) =>
                {
                    input.Move         = move;
                    input.ChatOpen     = chatOpen;   // persistenter Zustand 0/1
                    input.NetStatsHeld = statsHeld;  // halten
                })
                .Run();
        }
    }
}
