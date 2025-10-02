using Unity.Entities;

namespace BastionFall.Features.Time.Shared
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