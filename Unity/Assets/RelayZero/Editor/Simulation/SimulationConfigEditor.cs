using System;
using System.Globalization;
using System.Text;
using RelayZero.Foundation;
using RelayZero.Simulation;
using RelayZero.Simulation.Authoring;
using UnityEditor;
using UnityEngine;

namespace RelayZero.Editor.Simulation
{
    public static class SimulationConfigEditor
    {
        public const string MatchConfigAssetPath = "Assets/RelayZero/Config/Simulation/MatchConfig.asset";

        [MenuItem("Relay Zero/Simulation/Validate Configuration")]
        public static void ValidateConfigurationMenu()
        {
            Debug.Log(ValidateConfiguration());
        }

        public static string ValidateConfiguration()
        {
            MatchConfigAsset asset = AssetDatabase.LoadAssetAtPath<MatchConfigAsset>(MatchConfigAssetPath);
            if (asset == null)
            {
                throw new InvalidOperationException(
                    "No match configuration exists at " + MatchConfigAssetPath + ".");
            }

            MatchConfig compiled = asset.Compile();
            PlayerConfigAsset player = asset.PlayerConfig;
            CoreConfigAsset core = asset.CoreConfig;
            RegulationConfigAsset regulation = asset.RegulationConfig;
            StringBuilder report = new StringBuilder(2048);
            report.AppendLine("RELAY ZERO SIMULATION CONFIGURATION: PASS");
            report.Append("Schema: ").Append(compiled.SchemaVersion)
                .Append(" | ConfigVersion: ").AppendLine(compiled.Version.ToString());
            report.Append("Simulation: ").Append(SimulationTime.TicksPerSecond)
                .AppendLine(" Hz (exact conceptual tick = 1/60 s)");
            report.AppendLine("Movement configuration:");
            report.Append("  Normal maximum: ").Append(Format(player.NormalMaximumSpeedMetersPerSecond)).AppendLine(" m/s");
            report.Append("  Carrier maximum: ").Append(Format(player.CarrierMaximumSpeedMetersPerSecond)).AppendLine(" m/s");
            report.Append("  Acceleration: ").Append(Format(player.AccelerationMetersPerSecondSquared)).AppendLine(" m/s^2");
            report.Append("  Deceleration: ").Append(Format(player.DecelerationMetersPerSecondSquared)).AppendLine(" m/s^2");
            report.Append("  Player radius: ").Append(Format(player.RadiusMeters)).AppendLine(" m");
            report.Append("  Turn speed: ").Append(Format(player.TurnSpeedDegreesPerSecond)).AppendLine(" degrees/s");
            report.AppendLine("Relay Core configuration:");
            report.Append("  Pickup range: ").Append(Format(core.InteractionRangeMeters)).AppendLine(" m");
            report.Append("  Radius: ").Append(Format(core.RadiusMeters)).AppendLine(" m");
            report.Append("  Manual drop: ").Append(Format(core.ManualDropSpeedMetersPerSecond)).AppendLine(" m/s");
            report.Append("  Bounce: restitution ").Append(Format(core.Restitution))
                .Append(" | tangent retention ").AppendLine(Format(core.TangentialVelocityRetention));
            report.AppendLine("Loose-core movement values:");
            report.Append("  Speed cap: ").Append(Format(core.MaximumLooseSpeedMetersPerSecond)).AppendLine(" m/s");
            report.Append("  Exponential drag: ").Append(Format(core.PlanarDragPerSecond)).AppendLine(" /s");
            report.Append("  Rest: below ").Append(Format(core.RestSpeedThresholdMetersPerSecond))
                .Append(" m/s for ").Append(Format(core.RestQualificationSeconds)).AppendLine(" s");
            report.AppendLine("Regulation configuration:");
            report.Append("  Score target: ").Append(regulation.ScoreTargetPoints)
                .Append(" points = ").Append(compiled.Regulation.ScoreTargetMilliPoints).AppendLine(" milli-points");
            report.Append("  Base scoring: ").Append(regulation.BaseScoreMilliPointsPerSecond)
                .AppendLine(" milli-points/s");
            report.Append("  Active Relay scoring: ").Append(regulation.ActiveRelayScoreMilliPointsPerSecond)
                .AppendLine(" milli-points/s total");
            report.AppendLine("Duration conversion preview (ceiling):");
            AppendDuration(report, "Countdown", regulation.CountdownSeconds);
            AppendDuration(report, "Regulation", regulation.RegulationSeconds);
            AppendDuration(report, "Possession grace", regulation.PossessionGraceSeconds);
            AppendDuration(report, "Finalizing", regulation.FinalizingSeconds);
            AppendDuration(report, "Dash", 0.16d);
            AppendDuration(report, "Pulse anticipation", 0.18d);
            report.AppendLine("  Barrier visual rise: 0.08 s -> presentation only (collision is same accepted tick)");
            AppendDuration(report, "Forced-drop pickup lock", core.ForcedDropPickupLockSeconds);
            AppendDuration(report, "Manual-drop opponent lock", core.ManualDropOpponentPickupLockSeconds);
            AppendDuration(report, "Manual-drop previous-owner lock", core.ManualDropPreviousOwnerPickupLockSeconds);
            AppendDuration(report, "Core invalid/unreachable", core.InvalidResetDelaySeconds);
            AppendDuration(report, "Core resetting", core.ResettingDurationSeconds);
            AppendDuration(report, "Core center lock", core.CenterLockDurationSeconds);
            AppendDuration(report, "Shock stun", 0.65d);
            AppendDuration(report, "Terminal channel", 0.9d);
            AppendDuration(report, "Power transition", 1d);
            return report.ToString();
        }

        private static void AppendDuration(StringBuilder report, string label, double seconds)
        {
            report.Append("  ").Append(label).Append(": ")
                .Append(seconds.ToString("0.##", CultureInfo.InvariantCulture))
                .Append(" s -> ").Append(SimulationTime.SecondsToTicksCeiling(seconds))
                .AppendLine(" ticks");
        }

        private static void AppendDuration(StringBuilder report, string label, float seconds)
        {
            report.Append("  ").Append(label).Append(": ")
                .Append(seconds.ToString("0.##", CultureInfo.InvariantCulture))
                .Append(" s -> ").Append(SimulationTime.SecondsToTicksCeiling(seconds))
                .AppendLine(" ticks");
        }

        private static string Format(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
