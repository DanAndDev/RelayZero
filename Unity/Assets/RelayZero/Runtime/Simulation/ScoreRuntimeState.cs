using RelayZero.Foundation;

namespace RelayZero.Simulation
{
    public struct ScoreRuntimeState
    {
        public int PlayerZeroMilliPoints { get; internal set; }

        public int PlayerOneMilliPoints { get; internal set; }

        public int PlayerZeroRateRemainder { get; internal set; }

        public int PlayerOneRateRemainder { get; internal set; }

        public int GetMilliPoints(PlayerSlot slot)
        {
            return slot == PlayerSlot.Zero ? PlayerZeroMilliPoints : PlayerOneMilliPoints;
        }

        public int GetDisplayedPoints(PlayerSlot slot)
        {
            return GetMilliPoints(slot) / RegulationConfig.MilliPointsPerPoint;
        }

        public int GetRateRemainder(PlayerSlot slot)
        {
            return slot == PlayerSlot.Zero ? PlayerZeroRateRemainder : PlayerOneRateRemainder;
        }

        internal void Set(PlayerSlot slot, int milliPoints, int rateRemainder)
        {
            if (slot == PlayerSlot.Zero)
            {
                PlayerZeroMilliPoints = milliPoints;
                PlayerZeroRateRemainder = rateRemainder;
            }
            else
            {
                PlayerOneMilliPoints = milliPoints;
                PlayerOneRateRemainder = rateRemainder;
            }
        }
    }
}
