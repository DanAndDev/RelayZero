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
            StringBuilder report = new StringBuilder(768);
            report.AppendLine("RELAY ZERO SIMULATION CONFIGURATION: PASS");
            report.Append("Schema: ").Append(compiled.SchemaVersion)
                .Append(" | ConfigVersion: ").AppendLine(compiled.Version.ToString());
            report.Append("Simulation: ").Append(SimulationTime.TicksPerSecond)
                .AppendLine(" Hz (exact conceptual tick = 1/60 s)");
            report.AppendLine("Movement values (GDD 9.2 / 39.2):");
            report.Append("  Normal maximum: ").Append(Format(player.NormalMaximumSpeedMetersPerSecond)).AppendLine(" m/s");
            report.Append("  Carrier maximum: ").Append(Format(player.CarrierMaximumSpeedMetersPerSecond)).AppendLine(" m/s");
            report.Append("  Acceleration: ").Append(Format(player.AccelerationMetersPerSecondSquared)).AppendLine(" m/s^2");
            report.Append("  Deceleration: ").Append(Format(player.DecelerationMetersPerSecondSquared)).AppendLine(" m/s^2");
            report.Append("  Player radius: ").Append(Format(player.RadiusMeters)).AppendLine(" m");
            report.Append("  Turn speed: ").Append(Format(player.TurnSpeedDegreesPerSecond)).AppendLine(" degrees/s");
            report.AppendLine("Duration conversion preview (ceiling; TDD 11.6):");
            AppendDuration(report, "Dash", 0.16d);
            AppendDuration(report, "Pulse anticipation", 0.18d);
            report.AppendLine("  Barrier visual rise: 0.08 s -> presentation only (collision is same accepted tick)");
            AppendDuration(report, "Possession grace", 0.5d);
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

        private static string Format(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
