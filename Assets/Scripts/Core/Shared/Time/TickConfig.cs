using Unity.Mathematics;

namespace BastionFall.Core.Shared.Time
{
    public static class TickDefaults
    {
        public static float InGameTicksPerSecond = 8f;
        public static readonly float SystemTicksPerSecond = 30f;

        public static readonly float GlobalSpeed = 1f;
        public static readonly float GameSpeed = 1f;
        public static readonly float BiomeSpeed = 1f;
        public static readonly float TechSpeed = 1f;

        public static float InGameSecondsPerTick => 1f / math.max(0.0001f, InGameTicksPerSecond);

        public static float InGameSpeedMultiplier => GlobalSpeed * GameSpeed * BiomeSpeed * TechSpeed;

        public static void SetInGameMsPerTick(float ms)
        {
            InGameTicksPerSecond = 1000f / math.max(0.1f, ms);
        }
    }
}