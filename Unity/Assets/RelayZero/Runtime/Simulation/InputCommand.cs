using System;
using RelayZero.Foundation;
using Unity.Mathematics;

namespace RelayZero.Simulation
{
    public readonly struct InputCommand
    {
        public InputCommand(SimulationTick clientTick, CommandSequence sequence, float2 move)
            : this(
                clientTick,
                sequence,
                move,
                float2.zero,
                InputButtons.None,
                InputButtons.None,
                SimulationTick.Zero,
                0u)
        {
        }

        public InputCommand(
            SimulationTick clientTick,
            CommandSequence sequence,
            float2 move,
            float2 aim,
            InputButtons pressedButtons,
            InputButtons heldButtons,
            SimulationTick lastReceivedServerTick,
            uint lastReceivedSnapshotSequence)
        {
            if (!TryNormalizeMove(move, out float2 normalizedMove))
            {
                throw new ArgumentOutOfRangeException(nameof(move), "Movement input must be finite.");
            }

            if (!TryNormalizeAim(aim, out float2 normalizedAim))
            {
                throw new ArgumentOutOfRangeException(nameof(aim), "Aim input must be finite.");
            }

            if (!AreButtonsValid(pressedButtons))
            {
                throw new ArgumentOutOfRangeException(nameof(pressedButtons), "Pressed buttons contain undefined flags.");
            }

            if (!AreButtonsValid(heldButtons))
            {
                throw new ArgumentOutOfRangeException(nameof(heldButtons), "Held buttons contain undefined flags.");
            }

            ClientTick = clientTick;
            Sequence = sequence;
            Move = normalizedMove;
            Aim = normalizedAim;
            PressedButtons = pressedButtons;
            HeldButtons = heldButtons;
            LastReceivedServerTick = lastReceivedServerTick;
            LastReceivedSnapshotSequence = lastReceivedSnapshotSequence;
        }

        public SimulationTick ClientTick { get; }

        public CommandSequence Sequence { get; }

        public float2 Move { get; }

        public float2 Aim { get; }

        public InputButtons PressedButtons { get; }

        public InputButtons HeldButtons { get; }

        public SimulationTick LastReceivedServerTick { get; }

        public uint LastReceivedSnapshotSequence { get; }

        public static bool TryCreate(
            SimulationTick clientTick,
            CommandSequence sequence,
            float2 move,
            out InputCommand command)
        {
            return TryCreate(
                clientTick,
                sequence,
                move,
                float2.zero,
                InputButtons.None,
                InputButtons.None,
                SimulationTick.Zero,
                0u,
                out command);
        }

        public static bool TryCreate(
            SimulationTick clientTick,
            CommandSequence sequence,
            float2 move,
            float2 aim,
            InputButtons pressedButtons,
            InputButtons heldButtons,
            SimulationTick lastReceivedServerTick,
            uint lastReceivedSnapshotSequence,
            out InputCommand command)
        {
            if (!TryNormalizeMove(move, out float2 normalizedMove))
            {
                command = default;
                return false;
            }

            if (!TryNormalizeAim(aim, out float2 normalizedAim) ||
                !AreButtonsValid(pressedButtons) ||
                !AreButtonsValid(heldButtons))
            {
                command = default;
                return false;
            }

            command = new InputCommand(
                clientTick,
                sequence,
                normalizedMove,
                normalizedAim,
                pressedButtons,
                heldButtons,
                lastReceivedServerTick,
                lastReceivedSnapshotSequence,
                true);
            return true;
        }

        private InputCommand(
            SimulationTick clientTick,
            CommandSequence sequence,
            float2 normalizedMove,
            float2 normalizedAim,
            InputButtons pressedButtons,
            InputButtons heldButtons,
            SimulationTick lastReceivedServerTick,
            uint lastReceivedSnapshotSequence,
            bool _)
        {
            ClientTick = clientTick;
            Sequence = sequence;
            Move = normalizedMove;
            Aim = normalizedAim;
            PressedButtons = pressedButtons;
            HeldButtons = heldButtons;
            LastReceivedServerTick = lastReceivedServerTick;
            LastReceivedSnapshotSequence = lastReceivedSnapshotSequence;
        }

        public static bool TryNormalizeMove(float2 move, out float2 normalizedMove)
        {
            if (!math.all(math.isfinite(move)))
            {
                normalizedMove = float2.zero;
                return false;
            }

            float maximumComponent = math.cmax(math.abs(move));
            if (maximumComponent > 1f)
            {
                float2 scaled = move / maximumComponent;
                normalizedMove = scaled * math.rsqrt(math.lengthsq(scaled));
                return true;
            }

            float lengthSquared = math.lengthsq(move);
            normalizedMove = lengthSquared > 1f ? move * math.rsqrt(lengthSquared) : move;
            return true;
        }

        public static bool TryNormalizeAim(float2 aim, out float2 normalizedAim)
        {
            if (!math.all(math.isfinite(aim)))
            {
                normalizedAim = float2.zero;
                return false;
            }

            float maximumComponent = math.cmax(math.abs(aim));
            if (maximumComponent == 0f)
            {
                normalizedAim = float2.zero;
                return true;
            }

            float2 scaled = aim / maximumComponent;
            normalizedAim = scaled * math.rsqrt(math.lengthsq(scaled));
            return true;
        }

        public static bool AreButtonsValid(InputButtons buttons)
        {
            return (buttons & ~InputButtons.All) == InputButtons.None;
        }
    }
}
