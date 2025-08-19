// Assets/Scripts/ServerOnly/ServerInputLogSystem.cs
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using ECSPlayerInput = Game.Shared.Authoring.PlayerInput;
using Game.Shared.Util;

namespace Game.Server
{
    public struct ServerInputDebugState : IComponentData
    {
        public double NextLogTime;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct ServerInputLogSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.CreateSingleton(new ServerInputDebugState { NextLogTime = 0 });
        }

        public void OnUpdate(ref SystemState state)
        {
            double time = SystemAPI.Time.ElapsedTime;
            ref var sdbg = ref SystemAPI.GetSingletonRW<ServerInputDebugState>().ValueRW;

            if (time < sdbg.NextLogTime) return;
            sdbg.NextLogTime = time + 1.5;

            foreach (var (inp, owner) in SystemAPI.Query<RefRO<ECSPlayerInput>, RefRO<GhostOwner>>())
            {
                float h = inp.ValueRO.Horizontal;
                float v = inp.ValueRO.Vertical;
                bool j = inp.ValueRO.Jump.IsSet;
                if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f || j)
                {
                    state.LogInfo("Input", "netId={0} h={1:0.00} v={2:0.00} j={3}", owner.ValueRO.NetworkId, h, v, j ? 1 : 0);
                    break;
                }
            }
        }
    }
}