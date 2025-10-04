using System.Collections.Generic;
using UnityEngine;

namespace BastionFall.Features.Chat.Client.Systems
{
    
    public static class ChatClientLocalHistory
    {
        private static readonly List<string> _lines = new();
        private static int    _relIndex = -1;
        private static string _draft    = "";

        public static void Push(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
                _lines.Add(text);
            _relIndex = -1;
            _draft    = "";
        }

        
        public static string Step(int delta, string current)
        {
            if (_lines.Count == 0) return current;

            if (_relIndex == -1) _draft = current; // Draft sichern beim ersten Navigieren

            var next = _relIndex + (delta < 0 ? 1 : -1);
            next = Mathf.Clamp(next, -1, _lines.Count - 1);
            _relIndex = next;

            if (_relIndex == -1) return _draft;

            var idx = _lines.Count - 1 - _relIndex; // 0 => jüngste Zeile
            return _lines[idx];
        }

        public static void Clear()
        {
            _lines.Clear();
            _relIndex = -1;
            _draft    = "";
        }
    }
}