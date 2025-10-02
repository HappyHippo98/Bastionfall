using Unity.NetCode;

namespace BastionFall.Features.Player.Shared.RPC
{
    public struct ReportNetStatsRpc : IRpcCommand
    {
        public int NetworkId;
        public float RttMs;
        public int Fps;
        public int ConnectionSeconds;
    }
}