// Assets/Scripts/Shared/Util/EnsureRunInBackground.cs
using UnityEngine;

namespace Game.Shared.Util
{
    public static class EnsureRunInBackground
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void Apply()
        {
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
        }
    }
}