using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Shared.Authoring.Appearance;
using Unity.Rendering;

namespace ClientOnly.Appearance
{
    /// <summary>
    /// Spiegelt die Ghost-Farbe (PlayerRenderColor) auf das URP-Per-Entity-Material-Property,
    /// damit die Kapsel tatsächlich ihre Farbe ändert.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(GhostSimulationSystemGroup))] // nach Snapshot-Apply
    [BurstCompile]
    public partial struct ClientApplyPlayerColorSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState s)
        {
            // Nur laufen, wenn die nötigen Komponenten existieren
            s.RequireForUpdate<PlayerRenderColor>();
            s.RequireForUpdate<URPMaterialPropertyBaseColor>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState s)
        {
            // Nur updaten, wenn die Ghost-Farbe sich geändert hat
            foreach (var (ghostCol, baseCol) in SystemAPI
                         .Query<RefRO<PlayerRenderColor>, RefRW<URPMaterialPropertyBaseColor>>()
                         .WithChangeFilter<PlayerRenderColor>())
            {
                float4 rgba = ghostCol.ValueRO.Rgba;
                baseCol.ValueRW.Value = rgba;
            }
        }
    }
}
