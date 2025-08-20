// Assets/Scripts/Shared/Util/BFLog.cs
using System.Runtime.CompilerServices;
using Unity.Entities;
using UnityEngine;

namespace Game.Shared.Util
{
    public static class BfLog
    {
        // Direkt ohne SystemState (z.B. in MonoBehaviours)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Info(string cat, string msg)
        {
#if UNITY_DEBUG || UNITY_EDITOR
            Debug.Log($"[{cat}] {msg}");
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Warn(string cat, string msg)
        {
#if UNITY_DEBUG || UNITY_EDITOR
            Debug.LogWarning($"[{cat}] {msg}");
#endif
        }

        // Fehler wollen wir auch im Release sehen
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(string cat, string msg)
            => Debug.LogError($"[{cat}] {msg}");

        // Mit World-Name (für DOTS Systems)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Info(ref SystemState s, string cat, string msg)
        {
#if UNITY_DEBUG || UNITY_EDITOR
            Debug.Log($"[{s.WorldUnmanaged.Name}][{cat}] {msg}");
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Warn(ref SystemState s, string cat, string msg)
        {
#if UNITY_DEBUG || UNITY_EDITOR
            Debug.LogWarning($"[{s.WorldUnmanaged.Name}][{cat}] {msg}");
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(ref SystemState s, string cat, string msg)
            => Debug.LogError($"[{s.WorldUnmanaged.Name}][{cat}] {msg}");

        // „Once“-Logger: nur in Debug/Editor sinnvoll
        static readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> _once = new();
        public static void InfoOnce(string key, string cat, string msg)
        {
#if UNITY_DEBUG || UNITY_EDITOR
            if (_once.TryAdd(key, 1)) Debug.Log($"[{cat}] {msg}");
#endif
        }
    }

    // Komfort: Extension für Systems mit Format-String
    public static class BFLogExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogInfo(this ref SystemState s, string cat, string fmt, params object[] args)
        {
#if UNITY_DEBUG || UNITY_EDITOR
            Debug.Log($"[{s.WorldUnmanaged.Name}][{cat}] {string.Format(fmt, args)}");
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogWarn(this ref SystemState s, string cat, string fmt, params object[] args)
        {
#if UNITY_DEBUG || UNITY_EDITOR
            Debug.LogWarning($"[{s.WorldUnmanaged.Name}][{cat}] {string.Format(fmt, args)}");
#endif
        }

        // Fehler weiterhin sichtbar
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogError(this ref SystemState s, string cat, string fmt, params object[] args)
            => Debug.LogError($"[{s.WorldUnmanaged.Name}][{cat}] {string.Format(fmt, args)}");
    }
}
