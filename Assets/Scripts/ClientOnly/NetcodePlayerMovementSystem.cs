using ClientOnly.Authoring;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace ClientOnly
{
    
    
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    partial struct NetcodePlayerMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (playerInput, lt) in 
                     SystemAPI.Query<RefRO<NetcodePlayerInput>,RefRW<LocalTransform>>().WithAll<Simulate>())
            {
                float moveSpeed = 10f;
                float3 moveVector = new float3(playerInput.ValueRO.InputVector.x,0,playerInput.ValueRO.InputVector.y);
                lt.ValueRW.Position += moveVector * moveSpeed * SystemAPI.Time.DeltaTime;
            }
        }
    }
}