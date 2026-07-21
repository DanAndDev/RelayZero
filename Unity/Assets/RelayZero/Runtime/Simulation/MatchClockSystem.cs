using System;
using RelayZero.Foundation;

namespace RelayZero.Simulation
{
    internal static class MatchClockSystem
    {
        public static bool BeginTick(
            ref MatchClockState clock,
            SimulationTick tick,
            MatchEventBuffer events)
        {
            if (clock.Phase == MatchPhase.Countdown && !clock.PhaseEndTick.IsNewerThan(tick))
            {
                EnterPhase(ref clock, MatchPhase.Regulation, tick, SimulationTick.Zero, events);
                return true;
            }

            if (clock.Phase == MatchPhase.Finalizing && !clock.PhaseEndTick.IsNewerThan(tick))
            {
                EnterPhase(ref clock, MatchPhase.Results, tick, SimulationTick.Zero, events);
                return false;
            }

            return clock.Phase == MatchPhase.Regulation;
        }

        public static void EndRegulationTick(
            ref MatchClockState clock,
            in ScoreRuntimeState score,
            in RegulationConfig config,
            SimulationTick tick,
            MatchEventBuffer events)
        {
            if (clock.Phase != MatchPhase.Regulation)
            {
                return;
            }

            if (clock.RegulationTicksRemaining == 0u)
            {
                throw new InvalidOperationException("Regulation clock cannot tick below zero.");
            }

            clock.RegulationTicksRemaining--;
            bool playerZeroAtTarget = score.PlayerZeroMilliPoints >= config.ScoreTargetMilliPoints;
            bool playerOneAtTarget = score.PlayerOneMilliPoints >= config.ScoreTargetMilliPoints;
            if (playerZeroAtTarget || playerOneAtTarget)
            {
                if (playerZeroAtTarget && playerOneAtTarget)
                {
                    throw new InvalidOperationException("Both players cannot reach the target with a single carried core.");
                }

                PlayerSlot winner = playerZeroAtTarget ? PlayerSlot.Zero : PlayerSlot.One;
                Finalize(
                    ref clock,
                    in score,
                    winner,
                    MatchResultReason.TargetScore,
                    in config,
                    tick,
                    events);
                return;
            }

            if (clock.RegulationTicksRemaining != 0u)
            {
                return;
            }

            if (score.PlayerZeroMilliPoints == score.PlayerOneMilliPoints)
            {
                EnterPhase(ref clock, MatchPhase.OvertimeReset, tick, SimulationTick.Zero, events);
                events.Add(MatchEventType.RegulationTieDetected);
                return;
            }

            PlayerSlot timerWinner = score.PlayerZeroMilliPoints > score.PlayerOneMilliPoints
                ? PlayerSlot.Zero
                : PlayerSlot.One;
            Finalize(
                ref clock,
                in score,
                timerWinner,
                MatchResultReason.RegulationTimer,
                in config,
                tick,
                events);
        }

        private static void Finalize(
            ref MatchClockState clock,
            in ScoreRuntimeState score,
            PlayerSlot winner,
            MatchResultReason reason,
            in RegulationConfig config,
            SimulationTick tick,
            MatchEventBuffer events)
        {
            if (clock.HasResult)
            {
                throw new InvalidOperationException("A finalized result cannot be replaced.");
            }

            clock.Result = new MatchResult(
                MatchResultOutcome.PlayerVictory,
                reason,
                winner,
                tick,
                score.PlayerZeroMilliPoints,
                score.PlayerOneMilliPoints,
                clock.RegulationTicksRemaining);
            clock.HasResult = true;
            EnterPhase(ref clock, MatchPhase.Finalizing, tick, tick + config.FinalizingDurationTicks, events);
            events.Add(MatchEventType.MatchFinalized, winner, (int)reason);
        }

        private static void EnterPhase(
            ref MatchClockState clock,
            MatchPhase phase,
            SimulationTick tick,
            SimulationTick endTick,
            MatchEventBuffer events)
        {
            clock.Phase = phase;
            clock.PhaseStartTick = tick;
            clock.PhaseEndTick = endTick;
            events.Add(MatchEventType.MatchPhaseChanged, (int)phase);
        }
    }
}
