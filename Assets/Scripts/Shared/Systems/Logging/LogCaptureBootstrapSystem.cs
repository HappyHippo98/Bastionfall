using Shared.Logging;
using Unity.Entities;

namespace Shared.Systems.Logging
{
    [WorldSystemFilter(
        WorldSystemFilterFlags.ClientSimulation |
        WorldSystemFilterFlags.ServerSimulation |
        WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct LogCaptureBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            AppLog.SetUnityCapture(true);
            AppLog.HookUnityCapture();
        }

        public void OnDestroy(ref SystemState state)
        {
            AppLog.SetUnityCapture(false);
        }

        public void OnUpdate(ref SystemState state)
        {
        }
    }
}