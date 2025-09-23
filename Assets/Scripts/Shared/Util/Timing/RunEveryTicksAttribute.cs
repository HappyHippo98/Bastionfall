using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;

namespace Shared.Util.Timing
{
    public enum TickSource
    {
        NetcodeServer,
        InGame
    }

    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, Inherited = false)]
    public class RunEveryTicksAttribute : Attribute
    {
        public readonly int IntervalTicks;
        public readonly TickSource Source;

        public RunEveryTicksAttribute(TickSource source, int intervalTicks)
        {
            Source = source;
            IntervalTicks = math.max(1, intervalTicks);
        }
    }

    public sealed class RunEveryServerTicksAttribute : RunEveryTicksAttribute
    {
        public RunEveryServerTicksAttribute(int intervalTicks) : base(TickSource.NetcodeServer, intervalTicks)
        {
        }
    }

    public sealed class RunEveryInGameTicksAttribute : RunEveryTicksAttribute
    {
        public RunEveryInGameTicksAttribute(int intervalTicks) : base(TickSource.InGame, intervalTicks)
        {
        }
    }

    public struct TickTimer
    {
        public int Interval;
        public long NextTick;
        public long StartTick;
        public long StopTick;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TickTimer Start(long nowTick, int interval, long stopTick = long.MaxValue)
        {
            if (interval < 1) interval = 1;
            return new TickTimer { Interval = interval, NextTick = nowTick, StartTick = nowTick, StopTick = stopTick };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDue(long nowTick)
        {
            if (nowTick < StartTick || nowTick > StopTick) return false;
            if (nowTick < NextTick) return false;
            NextTick = nowTick + Interval;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset(long nowTick, int? newInterval = null)
        {
            if (newInterval.HasValue) Interval = math.max(1, newInterval.Value);
            StartTick = nowTick;
            NextTick = nowTick + Interval;
        }
    }

    /// <summary>
    ///     Liest das Attribut von TSystem und prüft Fälligkeit.
    ///     WICHTIG: dieser Guard kennt keine SystemAPI – gib ihm den aktuellen Tick aus deinem System.
    /// </summary>
    public struct RunEveryTicksGuard<TSystem>
    {
        private bool _initialized;
        private TickTimer _timer;

        public TickSource Source { get; private set; }

        public int IntervalTicks => _timer.Interval;
        public long NextDueTick => _timer.NextTick;


        /// <summary>Einmalig aufrufen (z.B. in OnCreate). Liest das Attribut.</summary>
        [BurstDiscard]
        public void InitFromAttribute(int defaultInterval = 1, TickSource defaultSource = TickSource.NetcodeServer)
        {
            if (_initialized) return;

            var interval = defaultInterval;
            var src = defaultSource;

            var attr = (RunEveryTicksAttribute)Attribute.GetCustomAttribute(typeof(TSystem),
                typeof(RunEveryTicksAttribute));
            if (attr != null)
            {
                src = attr.Source;
                interval = math.max(1, attr.IntervalTicks);
            }

            Source = src;
            _timer = TickTimer.Start(0, interval); // "now" setzen wir erst beim ersten IsDue()
            _initialized = true;
        }

        /// <summary>Prüft Fälligkeit für den übergebenen Tick.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDue(long nowTick)
        {
            if (!_initialized) return false;
            // Falls der Timer noch auf "0" startet, beim ersten Call auf den aktuellen Tick setzen
            if (_timer.StartTick == 0 && _timer.NextTick == 0)
                _timer.Reset(nowTick, _timer.Interval);
            return _timer.IsDue(nowTick);
        }

        /// <summary>Zur Laufzeit Intervall ändern.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OverrideIntervalTicks(int newIntervalTicks, long nowTick)
        {
            _timer.Reset(nowTick, math.max(1, newIntervalTicks));
        }

        /// <summary>Quelle wechseln (z.B. InGame → Netcode) + optional neues Intervall.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetSource(TickSource newSource, long nowTick, int? newIntervalTicks = null)
        {
            Source = newSource;
            _timer.Reset(nowTick, newIntervalTicks.HasValue ? math.max(1, newIntervalTicks.Value) : _timer.Interval);
        }
    }
}