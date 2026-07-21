using System;
using RelayZero.Foundation;
using Unity.Mathematics;

namespace RelayZero.Simulation
{
    public readonly struct MatchInitialization
    {
        public MatchInitialization(
            MatchId matchId,
            ulong matchSeed,
            MatchRoster roster,
            float2 playerZeroPosition,
            float2 playerOnePosition)
            : this(
                matchId,
                matchSeed,
                roster,
                playerZeroPosition,
                playerOnePosition,
                new float2(0f, 1f),
                new float2(0f, -1f),
                MatchStartMode.Countdown)
        {
        }

        public MatchInitialization(
            MatchId matchId,
            ulong matchSeed,
            MatchRoster roster,
            float2 playerZeroPosition,
            float2 playerOnePosition,
            float2 playerZeroFacingDirection,
            float2 playerOneFacingDirection)
            : this(
                matchId,
                matchSeed,
                roster,
                playerZeroPosition,
                playerOnePosition,
                playerZeroFacingDirection,
                playerOneFacingDirection,
                MatchStartMode.Countdown)
        {
        }

        public MatchInitialization(
            MatchId matchId,
            ulong matchSeed,
            MatchRoster roster,
            float2 playerZeroPosition,
            float2 playerOnePosition,
            float2 playerZeroFacingDirection,
            float2 playerOneFacingDirection,
            MatchStartMode startMode)
        {
            if (!matchId.IsValid)
            {
                throw new ArgumentException("A valid match ID is required.", nameof(matchId));
            }

            if (!roster.PlayerZero.IsValid || !roster.PlayerOne.IsValid || roster.PlayerZero == roster.PlayerOne)
            {
                throw new ArgumentException("A valid two-player roster is required.", nameof(roster));
            }

            if (!IsFinite(playerZeroPosition))
            {
                throw new ArgumentOutOfRangeException(nameof(playerZeroPosition), "Initial position must be finite.");
            }

            if (!IsFinite(playerOnePosition))
            {
                throw new ArgumentOutOfRangeException(nameof(playerOnePosition), "Initial position must be finite.");
            }

            if (!TryNormalizeDirection(playerZeroFacingDirection, out float2 normalizedPlayerZeroFacing))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(playerZeroFacingDirection),
                    "Initial facing direction must be finite and non-zero.");
            }

            if (!TryNormalizeDirection(playerOneFacingDirection, out float2 normalizedPlayerOneFacing))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(playerOneFacingDirection),
                    "Initial facing direction must be finite and non-zero.");
            }

            if ((uint)startMode > (uint)MatchStartMode.Regulation)
            {
                throw new ArgumentOutOfRangeException(nameof(startMode));
            }

            MatchId = matchId;
            MatchSeed = matchSeed;
            Roster = roster;
            PlayerZeroPosition = playerZeroPosition;
            PlayerOnePosition = playerOnePosition;
            PlayerZeroFacingDirection = normalizedPlayerZeroFacing;
            PlayerOneFacingDirection = normalizedPlayerOneFacing;
            StartMode = startMode;
        }

        public MatchId MatchId { get; }

        public ulong MatchSeed { get; }

        public MatchRoster Roster { get; }

        public float2 PlayerZeroPosition { get; }

        public float2 PlayerOnePosition { get; }

        public float2 PlayerZeroFacingDirection { get; }

        public float2 PlayerOneFacingDirection { get; }

        public MatchStartMode StartMode { get; }

        internal void Validate()
        {
            if (!MatchId.IsValid)
            {
                throw new ArgumentException("A valid match ID is required.", nameof(MatchId));
            }

            if (!Roster.PlayerZero.IsValid || !Roster.PlayerOne.IsValid || Roster.PlayerZero == Roster.PlayerOne)
            {
                throw new ArgumentException("A valid two-player roster is required.", nameof(Roster));
            }

            if (!IsFinite(PlayerZeroPosition) || !IsFinite(PlayerOnePosition) ||
                !TryNormalizeDirection(PlayerZeroFacingDirection, out _) ||
                !TryNormalizeDirection(PlayerOneFacingDirection, out _) ||
                (uint)StartMode > (uint)MatchStartMode.Regulation)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(PlayerZeroPosition),
                    "Initial positions and facing directions must be finite and valid.");
            }
        }

        private static bool IsFinite(float2 value)
        {
            return math.all(math.isfinite(value));
        }

        private static bool TryNormalizeDirection(float2 value, out float2 normalized)
        {
            float lengthSquared = math.lengthsq(value);
            if (!IsFinite(value) || !math.isfinite(lengthSquared) || lengthSquared <= 0.00000001f)
            {
                normalized = float2.zero;
                return false;
            }

            normalized = value * math.rsqrt(lengthSquared);
            return true;
        }
    }
}
