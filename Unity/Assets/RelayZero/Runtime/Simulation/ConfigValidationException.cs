using System;

namespace RelayZero.Simulation
{
    public sealed class ConfigValidationException : Exception
    {
        public ConfigValidationException(string message)
            : base(message)
        {
        }
    }
}
