using Unity.Entities;
using UnityEngine;

namespace Game.Shared.Authoring
{
    public class GhostRegistryAuthoring : MonoBehaviour
    {
        public GameObject[] Prefabs; // z.B. Assets/Resources/Ghosts/Player.prefab
    }

    public struct GhostRegistry : IComponentData {}
    public struct GhostRegistryEntry : IBufferElementData { public Entity Prefab; }

    public class GhostRegistryBaker : Baker<GhostRegistryAuthoring>
    {
        public override void Bake(GhostRegistryAuthoring a)
        {
            var e = GetEntity(TransformUsageFlags.None);
            AddComponent<GhostRegistry>(e);
            var buf = AddBuffer<GhostRegistryEntry>(e);

            if (a.Prefabs != null)
            {
                foreach (var go in a.Prefabs)
                {
                    if (go == null) continue;
                    var ent = GetEntity(go, TransformUsageFlags.Dynamic);
                    buf.Add(new GhostRegistryEntry { Prefab = ent });
                }
            }
        }
    }
}
