using Unity.Entities;
using UnityEngine;

namespace Shared.Authoring.Monitoring
{
    // Steuert, wie oft der CLIENT seine Stats an den Server pusht.
    public class NetStatsConfigAuthoring : MonoBehaviour
    {
        [Tooltip("Client pusht periodisch FPS/RTT (Server schreibt ins Ghost).")]
        public bool active = true;

        [Tooltip("Intervall in Sekunden für Client-Reports.")]
        [Min(0.05f)] public float ReportInterval = 0.5f;

        class Baker : Baker<NetStatsConfigAuthoring>
        {
            public override void Bake(NetStatsConfigAuthoring a)
            {
                var e = GetEntity(TransformUsageFlags.None);
                AddComponent(e, new NetStatsConfig
                {
                    Active        = a.active ? (byte)1 : (byte)0,
                    ReportInterval = a.ReportInterval
                });
            }
        }
    }

    public struct NetStatsConfig : IComponentData
    {
        public byte  Active;
        public float ReportInterval;
    }
}