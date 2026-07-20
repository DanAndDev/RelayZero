using RelayZero.Foundation;

namespace RelayZero.Simulation
{
    public sealed class TickInputs
    {
        private InputCommand playerZero;
        private InputCommand playerOne;

        public InputCommand Get(PlayerSlot slot)
        {
            return slot == PlayerSlot.Zero ? playerZero : playerOne;
        }

        public void Set(PlayerSlot slot, in InputCommand command)
        {
            if (slot == PlayerSlot.Zero)
            {
                playerZero = command;
            }
            else
            {
                playerOne = command;
            }
        }

        public void Clear()
        {
            playerZero = default;
            playerOne = default;
        }
    }
}
