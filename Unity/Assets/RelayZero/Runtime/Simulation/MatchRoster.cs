using System;
using RelayZero.Foundation;

namespace RelayZero.Simulation
{
    public readonly struct MatchRoster
    {
        public MatchRoster(PlayerId playerZero, PlayerId playerOne)
        {
            if (!playerZero.IsValid)
            {
                throw new ArgumentException("Player zero requires a valid ID.", nameof(playerZero));
            }

            if (!playerOne.IsValid)
            {
                throw new ArgumentException("Player one requires a valid ID.", nameof(playerOne));
            }

            if (playerZero == playerOne)
            {
                throw new ArgumentException("The two roster slots require distinct player IDs.", nameof(playerOne));
            }

            PlayerZero = playerZero;
            PlayerOne = playerOne;
        }

        public PlayerId PlayerZero { get; }

        public PlayerId PlayerOne { get; }

        public PlayerId GetPlayer(PlayerSlot slot)
        {
            return slot == PlayerSlot.Zero ? PlayerZero : PlayerOne;
        }
    }
}
