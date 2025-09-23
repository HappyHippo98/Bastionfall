using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace ClientOnly.Authoring
{
    public class NetcodePlayerInputAuthoring : MonoBehaviour
    {
        private class A : Baker<NetcodePlayerInputAuthoring>
        {
            public override void Bake(NetcodePlayerInputAuthoring authoring)
            {
                Entity e = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(e,new NetcodePlayerInput());
            }
        }
        
    }

    public struct NetcodePlayerInput : IInputComponentData
    {
        public float2 InputVector;
    }
    
}