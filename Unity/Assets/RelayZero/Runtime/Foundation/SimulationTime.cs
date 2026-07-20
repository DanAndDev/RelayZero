using System;

namespace RelayZero.Foundation
{
    public static class SimulationTime
    {
        public const int TicksPerSecond = 60;
        public const double TickDurationSeconds = 1d / TicksPerSecond;

        public static uint SecondsToTicksCeiling(float authoredSeconds)
        {
            if (float.IsNaN(authoredSeconds) ||
                float.IsInfinity(authoredSeconds) ||
                authoredSeconds < 0f ||
                authoredSeconds > uint.MaxValue / (double)TicksPerSecond)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(authoredSeconds),
                    authoredSeconds,
                    "Duration must be finite and non-negative.");
            }

            // Decimal conversion preserves author-facing precision instead of ceiling binary storage noise.
            decimal authoredTicks = (decimal)authoredSeconds * TicksPerSecond;
            decimal ticks = decimal.Ceiling(authoredTicks);
            if (authoredSeconds > 0f && ticks == 0m)
            {
                ticks = 1m;
            }

            if (ticks > uint.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(authoredSeconds),
                    authoredSeconds,
                    "Duration exceeds the tick range.");
            }

            return (uint)ticks;
        }

        public static uint SecondsToTicksCeiling(double authoredSeconds)
        {
            if (double.IsNaN(authoredSeconds) ||
                double.IsInfinity(authoredSeconds) ||
                authoredSeconds < 0d ||
                authoredSeconds > uint.MaxValue / (double)TicksPerSecond)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(authoredSeconds),
                    authoredSeconds,
                    "Duration must be finite and non-negative.");
            }

            // Decimal conversion preserves author-facing precision instead of ceiling binary storage noise.
            decimal authoredTicks = (decimal)authoredSeconds * TicksPerSecond;
            decimal ticks = decimal.Ceiling(authoredTicks);
            if (authoredSeconds > 0d && ticks == 0m)
            {
                ticks = 1m;
            }

            return (uint)ticks;
        }
    }
}
