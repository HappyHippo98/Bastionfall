using ClientOnly.Authoring;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace ClientOnly
{
    
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    partial struct NetcodePlayerInputSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();   
            state.RequireForUpdate<NetcodePlayerInput>();   
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (RefRW<NetcodePlayerInput> playerInput in SystemAPI.Query<RefRW<NetcodePlayerInput>>().WithAll<GhostOwnerIsLocal>())
            {
                float2 inputVector = new float2();
                if (UnityEngine.Input.GetKey(KeyCode.W))
                {
                    inputVector.y += 1f;
                }if (UnityEngine.Input.GetKey(KeyCode.A))
                {
                    inputVector.x -= 1f;
                }if (UnityEngine.Input.GetKey(KeyCode.S))
                {
                    inputVector.y -= 1f;
                }if (UnityEngine.Input.GetKey(KeyCode.D))
                {
                    inputVector.x += 1f;
                }
                
                playerInput.ValueRW.InputVector = inputVector;

            }
        }
    }
}