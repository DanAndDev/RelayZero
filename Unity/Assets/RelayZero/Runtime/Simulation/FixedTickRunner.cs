using System;
using RelayZero.Foundation;

namespace RelayZero.Simulation
{
    public sealed class FixedTickRunner
    {
        public const int MaximumTicksPerAdvance = 8;
        public const double DegradedBacklogSeconds = 0.1d;

        private const double TickBoundaryEpsilon = 0.000000001d;

        private double accumulatedTicks;

        public int LastAdvanceTickCount { get; private set; }

        public double BacklogSeconds => accumulatedTicks * SimulationTime.TickDurationSeconds;

        public bool HasDegradedBacklog => BacklogSeconds > DegradedBacklogSeconds;

        public int PendingWholeTicks
        {
            get
            {
                if (accumulatedTicks + TickBoundaryEpsilon < 1d)
                {
                    return 0;
                }

                if (accumulatedTicks >= int.MaxValue)
                {
                    return int.MaxValue;
                }

                return (int)Math.Floor(accumulatedTicks + TickBoundaryEpsilon);
            }
        }

        public int Advance(double elapsedSeconds, IFixedTickStep tickStep)
        {
            if (double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds) || elapsedSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(elapsedSeconds),
                    elapsedSeconds,
                    "Elapsed time must be finite and non-negative.");
            }

            if (tickStep == null)
            {
                throw new ArgumentNullException(nameof(tickStep));
            }

            double nextAccumulator = accumulatedTicks + elapsedSeconds * SimulationTime.TicksPerSecond;
            if (double.IsInfinity(nextAccumulator))
            {
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds), "Elapsed time exceeds the runner range.");
            }

            accumulatedTicks = nextAccumulator;
            int dueTicks = PendingWholeTicks;
            int tickCount = Math.Min(dueTicks, MaximumTicksPerAdvance);
            LastAdvanceTickCount = 0;

            for (int index = 0; index < tickCount; index++)
            {
                tickStep.Step();
                accumulatedTicks -= 1d;
                LastAdvanceTickCount++;
            }

            if (accumulatedTicks < 0d && accumulatedTicks > -TickBoundaryEpsilon)
            {
                accumulatedTicks = 0d;
            }

            return LastAdvanceTickCount;
        }
    }
}
