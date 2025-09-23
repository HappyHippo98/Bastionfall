using Unity.Entities;

namespace Shared.Time
{
    public struct NetcodeTickState : IComponentData
    {
        public uint ServerTick;
    }

    public struct InGameTickConfig : IComponentData
    {
        public float SecondsPerTick;
    }

    public struct InGameTickState : IComponentData
    {
        public long Tick;
        public double Accumulator;
    }
}