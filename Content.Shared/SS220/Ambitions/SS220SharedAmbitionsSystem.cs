// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
// Adapted from CrystallEdge ambitions by TheShuEd and contributors (728cd277da).

using Robust.Shared.Localization;

namespace Content.Shared.SS220.Ambitions;

public abstract class SS220SharedAmbitionsSystem : EntitySystem
{
    [Dependency] private readonly ILocalizationManager _localization = default!;

    public string LocalizePrototypeName(LocId name)
    {
        return _localization.TryGetString(name, out var localized) ? localized : name;
    }
}
