// Assets/Scripts/ClientOnly/LogLocalInputSystem.cs
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using Unity.Transforms;
using ECSPlayerInput = Game.Shared.Authoring.PlayerInput;
using Game.Shared.Util;

namespace Game.Client
{
    public struct LocalInputDebugState : IComponentData
    {
        public float LastH, LastV;
        public byte LastJump;
        public double NextLogTime;
        public byte WarnedNoLocal;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    [UpdateAfter(typeof(GatherPlayerInputSystem))]
    public partial struct LogLocalInputSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.CreateSingleton(new LocalInputDebugState
            {
                LastH = 2, LastV = 2, LastJump = 0, NextLogTime = 0, WarnedNoLocal = 0
            });
        }

        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            ref var dbg = ref SystemAPI.GetSingletonRW<LocalInputDebugState>().ValueRW;
            double time = SystemAPI.Time.ElapsedTime;

            var q = em.CreateEntityQuery(
                ComponentType.ReadOnly<ECSPlayerInput>(),
                ComponentType.ReadOnly<GhostOwnerIsLocal>());

            using var ents = q.ToEntityArray(Allocator.Temp);
            if (ents.Length == 0)
            {
                if (dbg.WarnedNoLocal == 0 && time >= dbg.NextLogTime)
                {
                    state.LogInfo("Input", "Kein lokaler Player gefunden (noch nicht connected/spawned?).");
                    dbg.WarnedNoLocal = 1;
                    dbg.NextLogTime = time + 1.0;
                }
                return;
            }

            dbg.WarnedNoLocal = 0;

            var e = ents[0];
            var input = em.GetComponentData<ECSPlayerInput>(e);

            float h = input.Horizontal;
            float v = input.Vertical;
            bool jump = input.Jump.IsSet;

            bool changed = Mathf.Abs(h - dbg.LastH) > 0.01f
                           || Mathf.Abs(v - dbg.LastV) > 0.01f
                           || (jump ? (dbg.LastJump == 0) : (dbg.LastJump != 0));

            if (changed || time >= dbg.NextLogTime)
            {
                if (em.HasComponent<LocalTransform>(e))
                {
                    var xf = em.GetComponentData<LocalTransform>(e);
                    state.LogInfo("Input", "h={0:0.00} v={1:0.00} jump={2}  pos=({3:0.00},{4:0.00},{5:0.00})",
                        h, v, jump ? 1 : 0, xf.Position.x, xf.Position.y, xf.Position.z);
                }
                else
                {
                    state.LogInfo("Input", "h={0:0.00} v={1:0.00} jump={2}",
                        h, v, jump ? 1 : 0);
                }

                dbg.LastH = h;
                dbg.LastV = v;
                dbg.LastJump = (byte)(jump ? 1 : 0);
                dbg.NextLogTime = time + 1.5;
            }
        }
    }
}
