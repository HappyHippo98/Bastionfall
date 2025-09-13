using System.Linq;
using Shared.Authoring.Monitoring;
using Unity.Burst;
using Unity.Entities;

namespace ClientOnly.Debug
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct ListConnectedClientsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            int count = 0;
            foreach (RefRO<PlayerNetStatsData> data in SystemAPI.Query<RefRO<PlayerNetStatsData>>()) count++;
            UnityEngine.Debug.Log($"Player count: {count}");

            //int index = 0;
            //foreach (var data in )
            //{
            //    index++;
            //    
            //}
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}