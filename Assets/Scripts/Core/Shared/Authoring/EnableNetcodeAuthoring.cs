using Unity.Entities;
using UnityEngine;

namespace BastionFall.Core.Shared.Authoring
{
    public class EnableNetcodeAuthoring : MonoBehaviour
    {
        private class Baker : Baker<EnableNetcodeAuthoring>
        {
            public override void Bake(EnableNetcodeAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.None);
                AddComponent(e, new EnableNetcode());
            }
        }
    }

    public struct EnableNetcode : IComponentData
    {
    }
}