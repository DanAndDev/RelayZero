using System;
using System.Diagnostics.CodeAnalysis;

namespace RelayZero.Simulation
{
    [Flags]
    [SuppressMessage(
        "Design",
        "CA1028:Enum Storage should be Int32",
        Justification = "InputCommand buttons are specified as a ushort bit mask in memory and on the wire.")]
    public enum InputButtons : ushort
    {
        None = 0,
        Dash = 1 << 0,
        Pulse = 1 << 1,
        Barrier = 1 << 2,
        Interact = 1 << 3,
        ForfeitRequest = 1 << 4,
        All = Dash | Pulse | Barrier | Interact | ForfeitRequest,
    }
}
