// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
// Adapted from CrystallEdge ambitions by TheShuEd and contributors (728cd277da).

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Ambitions;

[Serializable, NetSerializable]
public sealed class SS220ToggleAmbitionsScreenEvent : EntityEventArgs;

[Serializable, NetSerializable]
public enum SS220AmbitionsUIKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public readonly record struct SS220AmbitionInfo(EntProtoId Prototype, string Title, string Description);

[Serializable, NetSerializable]
public sealed class SS220AmbitionsBuiState(
    List<SS220AmbitionInfo> ambitions, int rerolls, int maxAmbitions, int revision, string? error = null)
    : BoundUserInterfaceState
{
    public readonly List<SS220AmbitionInfo> Ambitions = ambitions;
    public readonly int Rerolls = rerolls;
    public readonly int MaxAmbitions = maxAmbitions;
    public readonly int Revision = revision;
    public readonly string? Error = error;
}

[Serializable, NetSerializable]
public sealed class SS220AmbitionCreateMessage(int revision) : BoundUserInterfaceMessage
{
    public readonly int Revision = revision;
}

[Serializable, NetSerializable]
public sealed class SS220AmbitionLockMessage(int revision) : BoundUserInterfaceMessage
{
    public readonly int Revision = revision;
}

[Serializable, NetSerializable]
public sealed class SS220AmbitionDeleteMessage(EntProtoId prototype, int revision) : BoundUserInterfaceMessage
{
    public readonly EntProtoId Prototype = prototype;
    public readonly int Revision = revision;
}
