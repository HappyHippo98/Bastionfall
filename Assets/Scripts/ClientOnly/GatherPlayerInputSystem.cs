// Assets/Scripts/ClientOnly/GatherPlayerInputSystem.cs
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using ECSPlayerInput = Game.Shared.Authoring.PlayerInput;

#if ENABLE_INPUT_SYSTEM
using UInput = UnityEngine.InputSystem;
#endif

namespace Game.Client
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial struct GatherPlayerInputSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ECSPlayerInput>();
        }

        public void OnUpdate(ref SystemState state)
        {
            float h = 0f, v = 0f; bool jump = false;

#if ENABLE_INPUT_SYSTEM
            var kb = UInput.Keyboard.current;
            if (kb != null)
            {
                h = (kb.dKey.isPressed ? 1 : 0) + (kb.aKey.isPressed ? -1 : 0);
                v = (kb.wKey.isPressed ? 1 : 0) + (kb.sKey.isPressed ? -1 : 0);
                jump = kb.spaceKey.wasPressedThisFrame;
            }
#endif
            if (Mathf.Approximately(h, 0f) && Mathf.Approximately(v, 0f))
            {
                h = Input.GetAxisRaw("Horizontal");
                v = Input.GetAxisRaw("Vertical");
                jump = jump || Input.GetKeyDown(KeyCode.Space);
            }

            // **WICHTIG**: am CommandTarget schreiben (kein EntityManager.GetComponentRW benutzen)
            if (SystemAPI.TryGetSingleton<CommandTarget>(out var ct)
                && ct.targetEntity != Entity.Null
                && state.EntityManager.HasComponent<ECSPlayerInput>(ct.targetEntity))
            {
                var rw = SystemAPI.GetComponentRW<ECSPlayerInput>(ct.targetEntity);
                ref var i = ref rw.ValueRW;
                i.Horizontal = Mathf.Clamp(h, -1f, 1f);
                i.Vertical   = Mathf.Clamp(v, -1f, 1f);
                if (jump) i.Jump.Set();
                return;
            }

            // Fallback: alle lokal besessenen Ghosts
            foreach (var input in SystemAPI
                     .Query<RefRW<ECSPlayerInput>>()
                     .WithAll<GhostOwnerIsLocal>())
            {
                ref var i = ref input.ValueRW;
                i.Horizontal = Mathf.Clamp(h, -1f, 1f);
                i.Vertical   = Mathf.Clamp(v, -1f, 1f);
                if (jump) i.Jump.Set();
            }
        }
    }
}
