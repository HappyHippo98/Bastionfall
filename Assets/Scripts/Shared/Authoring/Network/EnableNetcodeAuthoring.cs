using Unity.Entities;
using UnityEngine;

namespace Shared.Authoring.Network
{
    public class EnableNetcodeAuthoring : MonoBehaviour
    {
        class Baker : Baker<EnableNetcodeAuthoring>
        {
            public override void Bake(EnableNetcodeAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.None);
                AddComponent(e, new EnableNetcode());
            }
        }
    }

    public struct EnableNetcode : IComponentData {}
}