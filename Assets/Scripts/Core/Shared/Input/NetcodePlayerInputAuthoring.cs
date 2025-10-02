using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace BastionFall.Core.Shared.Input
{
    public class NetcodePlayerInputAuthoring : MonoBehaviour
    {
        private class A : Baker<NetcodePlayerInputAuthoring>
        {
            public override void Bake(NetcodePlayerInputAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(e, new NetcodePlayerInput());
            }
        }
    }


    public struct NetcodePlayerInput : IInputComponentData
    {
        public float2 Move;
        public byte ChatOpen;
        public byte NetStatsHeld;
    }
}