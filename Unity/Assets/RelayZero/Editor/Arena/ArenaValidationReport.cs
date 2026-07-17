using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RelayZero.Arena;
using RelayZero.Arena.Baking;

namespace RelayZero.Editor.Arena
{
    public sealed class ArenaValidationCheck
    {
        public ArenaValidationCheck(string code, string name, bool passed, string details)
        {
            Code = code ?? string.Empty;
            Name = name ?? string.Empty;
            Passed = passed;
            Details = details ?? string.Empty;
        }

        public string Code { get; }
        public string Name { get; }
        public bool Passed { get; }
        public string Details { get; }
    }

    public sealed class ArenaValidationReport
    {
        private readonly List<ArenaValidationCheck> checks = new List<ArenaValidationCheck>();

        public IReadOnlyList<ArenaValidationCheck> Checks => checks;
        public bool IsValid => checks.Count > 0 && checks.All(check => check.Passed);
        public int PassedCount => checks.Count(check => check.Passed);
        public int FailedCount => checks.Count(check => !check.Passed);

        public void Add(string code, string name, bool passed, string details)
        {
            checks.Add(new ArenaValidationCheck(code, name, passed, details));
        }

        public string FormatForLog()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(IsValid ? "SWITCHYARD VALIDATION PASSED" : "SWITCHYARD VALIDATION FAILED");
            foreach (ArenaValidationCheck check in checks)
            {
                builder.Append(check.Passed ? "[PASS] " : "[FAIL] ")
                    .Append(check.Code).Append(" ").Append(check.Name);
                if (!string.IsNullOrEmpty(check.Details))
                {
                    builder.Append(": ").Append(check.Details);
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }
    }

    public sealed class ArenaValidationResult
    {
        public ArenaValidationResult(
            ArenaValidationReport report,
            ArenaBakePayload payload,
            ArenaBakeData runtimeData,
            string contentHash)
        {
            Report = report ?? throw new ArgumentNullException(nameof(report));
            Payload = payload;
            RuntimeData = runtimeData;
            ContentHash = contentHash ?? string.Empty;
        }

        public ArenaValidationReport Report { get; }
        public ArenaBakePayload Payload { get; }
        public ArenaBakeData RuntimeData { get; }
        public string ContentHash { get; }
    }
}
