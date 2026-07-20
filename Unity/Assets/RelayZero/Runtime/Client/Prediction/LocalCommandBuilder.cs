using System;
using RelayZero.Foundation;
using RelayZero.Simulation;
using Unity.Mathematics;

namespace RelayZero.Client.Prediction
{
    public sealed class LocalCommandBuilder
    {
        public const float MinimumAimMagnitude = 0.0001f;

        private float2 latestMove;
        private float2 latestAim;
        private InputButtons pendingPressedButtons;
        private InputButtons heldButtons;
        private CommandSequence nextSequence;
        private SimulationTick lastReceivedServerTick;
        private uint lastReceivedSnapshotSequence;
        private bool hasValidAim;
        private bool hasExplicitAim;
        private bool gameplayBlocked;

        public LocalCommandBuilder()
            : this(CommandSequence.Zero)
        {
        }

        public LocalCommandBuilder(CommandSequence initialSequence)
        {
            nextSequence = initialSequence;
        }

        public float2 LatestMove
        {
            get { return latestMove; }
        }

        public float2 LatestAim
        {
            get { return latestAim; }
        }

        public bool HasValidAim
        {
            get { return hasValidAim; }
        }

        public bool HasExplicitAim
        {
            get { return hasExplicitAim; }
        }

        public InputButtons PendingPressedButtons
        {
            get { return pendingPressedButtons; }
        }

        public InputButtons HeldButtons
        {
            get { return heldButtons; }
        }

        public CommandSequence NextSequence
        {
            get { return nextSequence; }
        }

        public bool GameplayBlocked
        {
            get { return gameplayBlocked; }
        }

        public bool TrySetMove(float2 move)
        {
            if (!InputCommand.TryNormalizeMove(move, out float2 normalizedMove))
            {
                return false;
            }

            if (gameplayBlocked)
            {
                return false;
            }

            latestMove = normalizedMove;
            if (!hasExplicitAim && IsMeaningfulAim(normalizedMove))
            {
                InputCommand.TryNormalizeAim(normalizedMove, out latestAim);
                hasValidAim = true;
            }

            return true;
        }

        public bool TrySetAim(float2 aim)
        {
            if (gameplayBlocked ||
                !math.all(math.isfinite(aim)) ||
                !IsMeaningfulAim(aim) ||
                !InputCommand.TryNormalizeAim(aim, out float2 normalizedAim))
            {
                return false;
            }

            latestAim = normalizedAim;
            hasValidAim = true;
            hasExplicitAim = true;
            return true;
        }

        public void LatchPressed(InputButtons buttons)
        {
            ValidateButtons(buttons, nameof(buttons));
            if (!gameplayBlocked)
            {
                pendingPressedButtons |= buttons;
            }
        }

        public void SetHeldButtons(InputButtons buttons)
        {
            ValidateButtons(buttons, nameof(buttons));
            heldButtons = gameplayBlocked ? InputButtons.None : buttons;
        }

        public void SetButtonHeld(InputButtons button, bool isHeld)
        {
            ValidateButtons(button, nameof(button));
            if (gameplayBlocked)
            {
                heldButtons = InputButtons.None;
                return;
            }

            if (isHeld)
            {
                heldButtons |= button;
            }
            else
            {
                heldButtons &= ~button;
            }
        }

        public void SetGameplayBlocked(bool blocked)
        {
            if (blocked && !gameplayBlocked)
            {
                latestMove = float2.zero;
                pendingPressedButtons = InputButtons.None;
                heldButtons = InputButtons.None;
            }

            gameplayBlocked = blocked;
        }

        public void SetAcknowledgements(
            SimulationTick receivedServerTick,
            uint receivedSnapshotSequence)
        {
            lastReceivedServerTick = receivedServerTick;
            lastReceivedSnapshotSequence = receivedSnapshotSequence;
        }

        public InputCommand Build(SimulationTick clientTick)
        {
            float2 commandMove = gameplayBlocked ? float2.zero : latestMove;
            float2 commandAim = gameplayBlocked || !hasValidAim ? float2.zero : latestAim;
            InputButtons commandPressedButtons = gameplayBlocked
                ? InputButtons.None
                : pendingPressedButtons;
            InputButtons commandHeldButtons = gameplayBlocked
                ? InputButtons.None
                : heldButtons;

            pendingPressedButtons = InputButtons.None;

            InputCommand command = new InputCommand(
                clientTick,
                nextSequence,
                commandMove,
                commandAim,
                commandPressedButtons,
                commandHeldButtons,
                lastReceivedServerTick,
                lastReceivedSnapshotSequence);

            nextSequence = nextSequence.Next();
            return command;
        }

        private static bool IsMeaningfulAim(float2 aim)
        {
            return math.cmax(math.abs(aim)) >= MinimumAimMagnitude;
        }

        private static void ValidateButtons(InputButtons buttons, string parameterName)
        {
            if (!InputCommand.AreButtonsValid(buttons))
            {
                throw new ArgumentOutOfRangeException(parameterName, "Buttons contain undefined flags.");
            }
        }
    }
}
