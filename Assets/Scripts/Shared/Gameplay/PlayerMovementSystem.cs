// Assets/Scripts/Shared/Gameplay/PlayerMovementSystems.cs
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using Game.Shared.Authoring;

namespace Game.Shared.Gameplay
{
    // SERVER: autoritativ
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PlayerMovementServerSystem : ISystem
    {
        public void OnUpdate(ref SystemState s)
        {
            float dt = SystemAPI.Time.DeltaTime;
            foreach (var (lt, speed, input) in SystemAPI
                         .Query<RefRW<LocalTransform>, RefRO<MoveSpeed>, RefRO<PlayerInput>>()
                         .WithAll<PlayerTag>())
            {
                float2 dir = math.normalizesafe(new float2(input.ValueRO.Horizontal, input.ValueRO.Vertical));
                var t = lt.ValueRO;
                t.Position += new float3(dir.x, 0, dir.y) * speed.ValueRO.Value * dt;
                lt.ValueRW = t;
            }
        }
    }

    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct PlayerMovementPredictedSystem : ISystem
    {
        public void OnUpdate(ref SystemState s)
        {
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (lt, speed, input) in SystemAPI
                         .Query<RefRW<LocalTransform>, RefRO<MoveSpeed>, RefRO<PlayerInput>>()
                         .WithAll<PlayerTag, PredictedGhost>()) // nur predicted (lokaler Owner)
            {
                float2 dir = math.normalizesafe(new float2(input.ValueRO.Horizontal, input.ValueRO.Vertical));
                var t = lt.ValueRO;
                t.Position += new float3(dir.x, 0, dir.y) * speed.ValueRO.Value * dt;
                lt.ValueRW = t;
            }
        }
    }
}