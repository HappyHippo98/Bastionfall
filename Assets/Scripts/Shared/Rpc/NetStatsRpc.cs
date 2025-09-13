using Unity.Entities;
using Unity.NetCode;

namespace Shared.Rpc
{
    public struct ReportNetStatsRpc : IRpcCommand
    {
        public int   NetworkId;
        public float RttMs;
        public int Fps;
        public int ConnectionSeconds; 
    }
}