using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
// NEU:
using Unity.Rendering;

namespace Shared.Authoring.Appearance
{
    public class PlayerAppearanceAuthoring : MonoBehaviour
    {
        [Header("Startfarbe (nur Default, wird vom Server überschrieben)")]
        public Color startColor = Color.white;

        class Baker : Baker<PlayerAppearanceAuthoring>
        {
            public override void Bake(PlayerAppearanceAuthoring a)
            {
                var e = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(e, new PlayerRenderColor
                {
                    Rgba = new float4(a.startColor.r, a.startColor.g, a.startColor.b, a.startColor.a)
                });

                AddComponent(e, new URPMaterialPropertyBaseColor
                {
                    Value = new float4(a.startColor.r, a.startColor.g, a.startColor.b, a.startColor.a)
                });
            }
        }
    }

   
    [GhostComponent(PrefabType = GhostPrefabType.All)]
    public struct PlayerRenderColor : IComponentData
    {
        [GhostField] public float4 Rgba;
    }
}