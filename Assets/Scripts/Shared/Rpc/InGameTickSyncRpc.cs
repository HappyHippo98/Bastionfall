using Unity.NetCode;

namespace Shared.Rpc
{
    /// <summary>Server teilt periodisch den aktuellen InGame-Tick (und Config) mit.</summary>
    public struct InGameTickSyncRpc : IRpcCommand
    {
        public long Tick;
        public double Accumulator;
        public float SecondsPerTick;
    }
}