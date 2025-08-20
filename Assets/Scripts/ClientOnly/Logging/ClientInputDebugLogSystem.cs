// Assets/Scripts/ClientOnly/ClientInputDebugLogSystem.cs
#if UNITY_DEBUG
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using ECSPlayerInput = Game.Shared.Authoring.PlayerInput;

#if ENABLE_INPUT_SYSTEM
using UInput = UnityEngine.InputSystem;
#endif

namespace Game.Client
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    [UpdateAfter(typeof(GatherPlayerInputSystem))]
    public partial struct ClientInputDebugLogSystem : ISystem
    {
        private EntityQuery _localOwnerQ;
        private double _nextSample;
        private double _nextNoTargetWarn;
        private float _lastH, _lastV;
        private byte _lastJ;

        public void OnCreate(ref SystemState s)
        {
            _localOwnerQ = SystemAPI.QueryBuilder()
                .WithAll<ECSPlayerInput, GhostOwnerIsLocal>()
                .Build();
        }

        public void OnUpdate(ref SystemState s)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UInput.Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.wasPressedThisFrame) Debug.Log("[Input] W down");
                if (kb.aKey.wasPressedThisFrame) Debug.Log("[Input] A down");
                if (kb.sKey.wasPressedThisFrame) Debug.Log("[Input] S down");
                if (kb.dKey.wasPressedThisFrame) Debug.Log("[Input] D down");
                if (kb.spaceKey.wasPressedThisFrame) Debug.Log("[Input] SPACE down");
            }
#endif
            if (Input.GetKeyDown(KeyCode.W)) Debug.Log("[Input] W down (old)");
            if (Input.GetKeyDown(KeyCode.A)) Debug.Log("[Input] A down (old)");
            if (Input.GetKeyDown(KeyCode.S)) Debug.Log("[Input] S down (old)");
            if (Input.GetKeyDown(KeyCode.D)) Debug.Log("[Input] D down (old)");
            if (Input.GetKeyDown(KeyCode.Space)) Debug.Log("[Input] SPACE down (old)");

            var em = s.EntityManager;
            Entity target = Entity.Null;

            if (SystemAPI.TryGetSingleton<CommandTarget>(out var ct) &&
                ct.targetEntity != Entity.Null &&
                em.HasComponent<ECSPlayerInput>(ct.targetEntity))
            {
                target = ct.targetEntity;
            }
            else
            {
                using var ents = _localOwnerQ.ToEntityArray(Allocator.Temp);
                if (ents.Length > 0)
                    target = ents[0];
            }

            if (target == Entity.Null)
            {
                var t = SystemAPI.Time.ElapsedTime;
                if (t >= _nextNoTargetWarn)
                {
                    Debug.Log("[Input] Kein lokaler Player (GhostOwnerIsLocal) gefunden – Handshake/Spawn noch nicht fertig?");
                    _nextNoTargetWarn = t + 1.0f;
                }
                return;
            }

            var input = em.GetComponentData<ECSPlayerInput>(target);
            float h = input.Horizontal;
            float v = input.Vertical;
            byte j = (byte)(input.Jump.IsSet ? 1 : 0);

            bool changed = Mathf.Abs(h - _lastH) > 0.01f
                           || Mathf.Abs(v - _lastV) > 0.01f
                           || j != _lastJ;

            var now = SystemAPI.Time.ElapsedTime;
            if (changed || now >= _nextSample)
            {
                if (em.HasComponent<LocalTransform>(target))
                {
                    var xf = em.GetComponentData<LocalTransform>(target);
                    Debug.Log($"[Input] ECS h={h:0.00} v={v:0.00} jump={j}  pos=({xf.Position.x:0.00},{xf.Position.y:0.00},{xf.Position.z:0.00})");
                }
                else
                {
                    Debug.Log($"[Input] ECS h={h:0.00} v={v:0.00} jump={j}");
                }

                _lastH = h; _lastV = v; _lastJ = j;
                _nextSample = now + 1.5f;
            }
        }
    }
}
#endif
