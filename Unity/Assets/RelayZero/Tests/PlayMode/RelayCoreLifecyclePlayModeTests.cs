using System.Collections;
using NUnit.Framework;
using RelayZero.Client.Presentation;
using RelayZero.Simulation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RelayZero.Tests.PlayMode
{
    public sealed class RelayCoreLifecyclePlayModeTests
    {
        [UnityTest]
        public IEnumerator ScriptedSwitchyardLifecycleReturnsCoreToCenterLoose()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("Switchyard", LoadSceneMode.Single);
            while (!load.isDone)
            {
                yield return null;
            }

            SwitchyardMovementDemo demo = Object.FindAnyObjectByType<SwitchyardMovementDemo>(
                FindObjectsInactive.Include);
            Assert.That(demo, Is.Not.Null);
            Assert.That(demo.IsInitialized, Is.True);
            demo.SetInputMode(MovementDemoInputMode.ScriptedCoreLifecycle);

            const int maximumTicks = 900;
            for (int index = 0; index < maximumTicks && !demo.ScriptedLifecycleComplete; index++)
            {
                demo.Step();
            }

            Assert.That(demo.ScriptedLifecycleComplete, Is.True);
            Assert.That(demo.ScriptedContestedPickupComplete, Is.True);
            Assert.That(demo.ScriptedManualDropComplete, Is.True);
            Assert.That(demo.ScriptedOpponentPickupComplete, Is.True);
            Assert.That(demo.ScriptedForcedDropComplete, Is.True);
            Assert.That(demo.Simulation.State.Core.Mode, Is.EqualTo(CoreMode.Loose));
            Assert.That(demo.Simulation.State.Core.HasOwner, Is.False);
            Assert.That(demo.Simulation.State.Core.InvalidTickCount, Is.Zero);
            Assert.That(demo.Simulation.State.Core.LastDropReason, Is.EqualTo(CoreDropReason.Recovery));
            Assert.That(
                demo.Simulation.State.Core.Position,
                Is.EqualTo(demo.Simulation.Arena.CoreReset.Position));
        }
    }

    public sealed class RegulationResultsPlayModeTests
    {
        [UnityTest]
        public IEnumerator ScriptedTargetScoreEndsMatchWithTargetScoreResult()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("Switchyard", LoadSceneMode.Single);
            while (!load.isDone)
            {
                yield return null;
            }

            SwitchyardMovementDemo demo = FindInitializedDemo();
            demo.SetInputMode(MovementDemoInputMode.ScriptedTargetScore);

            int maximumTicks = GetMaximumRegulationLifecycleTicks(demo);
            for (int index = 0; index < maximumTicks && !demo.ScriptedTargetScoreComplete; index++)
            {
                demo.Step();
            }

            MatchClockState clock = demo.Simulation.State.Clock;
            Assert.That(demo.ScriptedTargetScoreComplete, Is.True);
            Assert.That(clock.Phase, Is.EqualTo(MatchPhase.Results));
            Assert.That(clock.HasResult, Is.True);
            Assert.That(clock.Result.IsFinal, Is.True);
            Assert.That(clock.Result.Reason, Is.EqualTo(MatchResultReason.TargetScore));
            Assert.That(clock.Result.Winner, Is.EqualTo(RelayZero.Foundation.PlayerSlot.Zero));
            Assert.That(
                clock.Result.PlayerZeroScoreMilliPoints,
                Is.EqualTo(demo.Simulation.Config.Regulation.ScoreTargetMilliPoints));
            Assert.That(clock.Result.PlayerOneScoreMilliPoints, Is.Zero);
            Assert.That(clock.Result.RegulationTicksRemaining, Is.GreaterThan(0u));
        }

        [UnityTest]
        public IEnumerator ScriptedTimerExpiryEndsMatchWithRegulationTimerResult()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("Switchyard", LoadSceneMode.Single);
            while (!load.isDone)
            {
                yield return null;
            }

            SwitchyardMovementDemo demo = FindInitializedDemo();
            demo.SetInputMode(MovementDemoInputMode.ScriptedTimerExpiry);

            int maximumTicks = GetMaximumRegulationLifecycleTicks(demo);
            for (int index = 0; index < maximumTicks && !demo.ScriptedTimerExpiryComplete; index++)
            {
                demo.Step();
            }

            MatchClockState clock = demo.Simulation.State.Clock;
            Assert.That(demo.ScriptedTimerExpiryComplete, Is.True);
            Assert.That(clock.Phase, Is.EqualTo(MatchPhase.Results));
            Assert.That(clock.HasResult, Is.True);
            Assert.That(clock.Result.IsFinal, Is.True);
            Assert.That(clock.Result.Reason, Is.EqualTo(MatchResultReason.RegulationTimer));
            Assert.That(clock.Result.Winner, Is.EqualTo(RelayZero.Foundation.PlayerSlot.Zero));
            Assert.That(clock.Result.PlayerZeroScoreMilliPoints, Is.EqualTo(5000));
            Assert.That(clock.Result.PlayerOneScoreMilliPoints, Is.Zero);
            Assert.That(clock.Result.RegulationTicksRemaining, Is.Zero);
        }

        [UnityTest]
        public IEnumerator TickBatchSetterAcceptsAllSupportedModes()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("Switchyard", LoadSceneMode.Single);
            while (!load.isDone)
            {
                yield return null;
            }

            SwitchyardMovementDemo demo = FindInitializedDemo();
            TickBatchMode[] supportedModes =
            {
                TickBatchMode.Realtime,
                TickBatchMode.TenTicksPerFrame,
                TickBatchMode.SixtyTicksPerFrame,
            };

            foreach (TickBatchMode mode in supportedModes)
            {
                demo.SetTickBatch(mode);
                Assert.That(demo.TickBatch, Is.EqualTo(mode));
            }
        }

        private static SwitchyardMovementDemo FindInitializedDemo()
        {
            SwitchyardMovementDemo demo = Object.FindAnyObjectByType<SwitchyardMovementDemo>(
                FindObjectsInactive.Include);
            Assert.That(demo, Is.Not.Null);
            Assert.That(demo.IsInitialized, Is.True);
            return demo;
        }

        private static int GetMaximumRegulationLifecycleTicks(SwitchyardMovementDemo demo)
        {
            RegulationConfig regulation = demo.Simulation.Config.Regulation;
            uint configuredTicks = regulation.CountdownDurationTicks +
                regulation.RegulationDurationTicks +
                regulation.FinalizingDurationTicks;
            return checked((int)configuredTicks + 600);
        }
    }
}
