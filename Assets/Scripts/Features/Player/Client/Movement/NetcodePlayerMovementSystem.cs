using BastionFall.Core.Shared.Input;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace BastionFall.Features.Player.Client.Movement
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct NetcodePlayerMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (input, lt) in
                     SystemAPI.Query<RefRO<NetcodePlayerInput>, RefRW<LocalTransform>>().WithAll<Simulate>())
            {
                const float moveSpeed = 10f;
                var moveVector = new float3(input.ValueRO.Move.x, 0, input.ValueRO.Move.y);
                lt.ValueRW.Position += moveVector * moveSpeed * SystemAPI.Time.DeltaTime;
            }
        }
    }
}