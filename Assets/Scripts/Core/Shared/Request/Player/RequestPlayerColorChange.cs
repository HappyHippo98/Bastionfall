using Unity.Entities;
using Unity.Mathematics;

namespace BastionFall.Core.Shared.Request.Player
{
    public struct RequestPlayerColorChange : IComponentData
    {
        public int OwnerNetworkId;
        public float4 Rgba;
    }
}