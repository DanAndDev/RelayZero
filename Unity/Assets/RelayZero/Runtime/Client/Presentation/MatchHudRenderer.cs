using System.Globalization;
using RelayZero.Foundation;
using RelayZero.Simulation;
using UnityEngine;

namespace RelayZero.Client.Presentation
{
    internal sealed class MatchHudRenderer
    {
        private GUIStyle topStyle = null!;
        private GUIStyle centerStyle = null!;
        private GUIStyle resultsStyle = null!;

        public void Draw(
            MatchState state,
            in RegulationConfig config,
            string scenario,
            string acceleration,
            bool showGo)
        {
            EnsureStyles();
            int zeroPoints = state.Score.GetDisplayedPoints(PlayerSlot.Zero);
            int onePoints = state.Score.GetDisplayedPoints(PlayerSlot.One);
            string timer = FormatClock(state.Clock.RegulationTicksRemaining);
            string possession = FormatPossession(state, in config);
            string header =
                "P0  " + zeroPoints + "   / " + config.ScoreTargetPoints + "   " + timer +
                "   " + onePoints + "  P1\n" +
                state.Clock.Phase + "  |  " + possession + "  |  " + scenario + "  |  " + acceleration;
            float width = Mathf.Min(860f, Screen.width - 24f);
            GUI.Box(new Rect((Screen.width - width) * 0.5f, 12f, width, 68f), header, topStyle);

            if (state.Clock.Phase == MatchPhase.Countdown)
            {
                uint remaining = state.Clock.GetCountdownTicksRemaining(state.Tick);
                uint numeral = (remaining + SimulationTime.TicksPerSecond - 1u) /
                    SimulationTime.TicksPerSecond;
                string countdown = numeral > 0u ? numeral.ToString(CultureInfo.InvariantCulture) : "GO";
                GUI.Label(new Rect(0f, Screen.height * 0.28f, Screen.width, 110f), countdown, centerStyle);
            }
            else if (showGo)
            {
                GUI.Label(new Rect(0f, Screen.height * 0.28f, Screen.width, 110f), "GO", centerStyle);
            }

            if (state.Clock.Phase == MatchPhase.OvertimeReset && !state.Clock.HasResult)
            {
                GUI.Box(
                    new Rect(Screen.width * 0.5f - 260f, Screen.height * 0.5f - 75f, 520f, 150f),
                    "REGULATION TIE\nOVERTIME RESET",
                    resultsStyle);
                return;
            }

            if (!state.Clock.HasResult)
            {
                return;
            }

            MatchResult result = state.Clock.Result;
            string outcome = result.Winner == PlayerSlot.Zero ? "VICTORY" : "DEFEAT";
            if (state.Clock.Phase == MatchPhase.Finalizing)
            {
                GUI.Label(new Rect(0f, Screen.height * 0.38f, Screen.width, 110f), outcome, centerStyle);
                return;
            }

            string reason = result.Reason == MatchResultReason.TargetScore
                ? "TARGET SCORE"
                : "REGULATION TIMER";
            string finalScore = FormatMilliPoints(result.PlayerZeroScoreMilliPoints) + " — " +
                FormatMilliPoints(result.PlayerOneScoreMilliPoints);
            GUI.Box(
                new Rect(Screen.width * 0.5f - 300f, Screen.height * 0.5f - 125f, 600f, 250f),
                outcome + "\n" + reason + "\n" + finalScore +
                "\nAuthoritative tick " + result.FinalizedTick.Value +
                "\nF7 — restart",
                resultsStyle);
        }

        private static string FormatPossession(MatchState state, in RegulationConfig config)
        {
            CoreRuntimeState core = state.Core;
            if (core.Mode != CoreMode.Carried || !core.HasOwner)
            {
                return "CORE LOOSE";
            }

            string owner = "P" + core.Owner.Index;
            if (state.Clock.Phase != MatchPhase.Regulation)
            {
                return owner + " POSSESSION — FROZEN";
            }

            PlayerState carrier = state.GetPlayer(core.Owner);
            if (carrier.ConnectionMode != PlayerConnectionMode.Connected)
            {
                return owner + " POSSESSION — DISCONNECTED";
            }

            uint grace = core.GetPossessionGraceRemainingTicks(state.Tick);
            if (grace == 0u)
            {
                return owner + " SCORING";
            }

            float seconds = grace / (float)SimulationTime.TicksPerSecond;
            return owner + " POSSESSION — GRACE " +
                seconds.ToString("0.00", CultureInfo.InvariantCulture) + "s / " +
                (config.PossessionGraceTicks / (float)SimulationTime.TicksPerSecond)
                    .ToString("0.00", CultureInfo.InvariantCulture) + "s";
        }

        private static string FormatClock(uint ticks)
        {
            uint seconds = (ticks + SimulationTime.TicksPerSecond - 1u) / SimulationTime.TicksPerSecond;
            return (seconds / 60u).ToString("00", CultureInfo.InvariantCulture) + ":" +
                (seconds % 60u).ToString("00", CultureInfo.InvariantCulture);
        }

        private static string FormatMilliPoints(int milliPoints)
        {
            return (milliPoints / (float)RegulationConfig.MilliPointsPerPoint)
                .ToString("0.000", CultureInfo.InvariantCulture);
        }

        private void EnsureStyles()
        {
            if (topStyle != null)
            {
                return;
            }

            topStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
            };
            centerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 64,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
            };
            resultsStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                wordWrap = true,
            };
        }
    }
}
