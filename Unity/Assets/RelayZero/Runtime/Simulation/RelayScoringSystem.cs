using RelayZero.Foundation;

namespace RelayZero.Simulation
{
    internal static class RelayScoringSystem
    {
        public static void Step(
            ref ScoreRuntimeState score,
            in CoreRuntimeState core,
            in PlayerState playerZero,
            in PlayerState playerOne,
            bool activeRelayBonusApplies,
            in RegulationConfig config,
            SimulationTick tick,
            MatchEventBuffer events)
        {
            if (core.Mode != CoreMode.Carried || !core.HasOwner ||
                core.PossessionGraceEndTick.IsNewerThan(tick))
            {
                return;
            }

            PlayerSlot carrierSlot = core.Owner;
            PlayerState carrier = carrierSlot == PlayerSlot.Zero ? playerZero : playerOne;
            if (carrier.ConnectionMode != PlayerConnectionMode.Connected)
            {
                return;
            }

            int rate = activeRelayBonusApplies
                ? config.ActiveRelayScoreMilliPointsPerSecond
                : config.BaseScoreMilliPointsPerSecond;
            int previousMilliPoints = score.GetMilliPoints(carrierSlot);
            int previousDisplayedPoints = previousMilliPoints / RegulationConfig.MilliPointsPerPoint;
            int numerator = checked(score.GetRateRemainder(carrierSlot) + rate);
            int addedMilliPoints = numerator / SimulationTime.TicksPerSecond;
            int remainder = numerator % SimulationTime.TicksPerSecond;
            int nextMilliPoints = checked(previousMilliPoints + addedMilliPoints);
            if (nextMilliPoints >= config.ScoreTargetMilliPoints)
            {
                nextMilliPoints = config.ScoreTargetMilliPoints;
            }

            score.Set(carrierSlot, nextMilliPoints, remainder);
            int displayedPoints = nextMilliPoints / RegulationConfig.MilliPointsPerPoint;
            if (displayedPoints != previousDisplayedPoints || nextMilliPoints == config.ScoreTargetMilliPoints)
            {
                events.Add(MatchEventType.ScoreChanged, carrierSlot, nextMilliPoints);
            }
        }
    }
}
