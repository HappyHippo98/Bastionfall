using Unity.Collections;
using Unity.Entities;

namespace Game.Shared.Authoring
{
    // Wird am gebackenen Prefab hängen und den Namen tragen
    public struct GhostName : IComponentData
    {
        public FixedString64Bytes Value;
    }

    // Einfacher Marker, den du am Ghost-Prefab anheftest

    // Baker läuft, sobald das Prefab den Marker hat
    public class GhostNameBaker : Baker<GhostNameMarker>
    {
        public override void Bake(GhostNameMarker a)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);
            FixedString64Bytes name = !string.IsNullOrEmpty(a.gameObject.name)
                ? (FixedString64Bytes)a.gameObject.name
                : (FixedString64Bytes)"Ghost";
            AddComponent(e, new GhostName { Value = name });
        }
    }
}