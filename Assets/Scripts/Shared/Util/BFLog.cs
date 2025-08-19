using System.Runtime.CompilerServices;
using Unity.Entities;
using UnityEngine;

namespace Game.Shared.Util
{
    public static class BFLog
    {
        // Direkt ohne SystemState (z.B. in MonoBehaviours)
        public static void Info  (string cat, string msg) => Debug.Log($"[{cat}] {msg}");
        public static void Warn  (string cat, string msg) => Debug.LogWarning($"[{cat}] {msg}");
        public static void Error (string cat, string msg) => Debug.LogError($"[{cat}] {msg}");

        // Mit World-Name (für DOTS Systems)
        public static void Info(ref SystemState s, string cat, string msg)
            => Debug.Log($"[{s.WorldUnmanaged.Name}][{cat}] {msg}");
        public static void Warn(ref SystemState s, string cat, string msg)
            => Debug.LogWarning($"[{s.WorldUnmanaged.Name}][{cat}] {msg}");
        public static void Error(ref SystemState s, string cat, string msg)
            => Debug.LogError($"[{s.WorldUnmanaged.Name}][{cat}] {msg}");

        // „Once“-Logger pro Key, um Spam zu vermeiden
        static readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> _once 
            = new();
        public static void InfoOnce(string key, string cat, string msg)
        {
            if (_once.TryAdd(key, 1)) Debug.Log($"[{cat}] {msg}");
        }
    }

    // Komfort: Extension für Systems mit Format-String
    public static class BFLogExtensions
    {
        public static void LogInfo(this ref SystemState s, string cat, string fmt, params object[] args)
            => Debug.Log($"[{s.WorldUnmanaged.Name}][{cat}] {string.Format(fmt, args)}");
        public static void LogWarn(this ref SystemState s, string cat, string fmt, params object[] args)
            => Debug.LogWarning($"[{s.WorldUnmanaged.Name}][{cat}] {string.Format(fmt, args)}");
        public static void LogError(this ref SystemState s, string cat, string fmt, params object[] args)
            => Debug.LogError($"[{s.WorldUnmanaged.Name}][{cat}] {string.Format(fmt, args)}");
    }
}
