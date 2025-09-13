using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Shared.Authoring.Monitoring
{
    public class PlayerNetStatsDataAuthoring : MonoBehaviour
    {
        [Tooltip("Falls der Player-Ghost noch keinen GhostOwner hat, füge einen hinzu.")]
        public bool ensureGhostOwner = true;
        class Baker : Baker<PlayerNetStatsDataAuthoring>
        {
            public override void Bake(PlayerNetStatsDataAuthoring a)
            {
                // WICHTIG: Dynamic, da es ein spawned Ghost ist
                var e = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(e, new PlayerNetStatsData
                {
                    NetworkId        = 0,
                    RttMs            = 0,
                    Fps              = 0,
                    ConnectionSeconds= 0
                });
                if (a.ensureGhostOwner)
                    AddComponent(e, new GhostOwner { NetworkId = 0 });
            }
        }
    }

    public struct PlayerNetStatsData : IComponentData
    {
        [GhostField] public int   NetworkId;
        [GhostField] public float RttMs;
        [GhostField(Quantization = 1)] public int Fps;
        [GhostField] public int ConnectionSeconds; // NEU
    }

}