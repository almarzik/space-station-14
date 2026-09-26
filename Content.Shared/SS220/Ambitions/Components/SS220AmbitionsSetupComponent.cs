// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
// Ported from CrystallEdge, TheShuEd and contributors, revision 728cd277da. Original authorship retained; SS220 notice covers adaptations.

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Ambitions.Components;

/// <summary>
/// Creates ambitious goals for the character and allows them to be rerolled a certain number of times.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), Access(typeof(SS220SharedAmbitionsSystem))]
public sealed partial class SS220AmbitionsSetupComponent : Component
{
    [DataField, AutoNetworkedField]
    public int RerollAmount = 10;

    [DataField, AutoNetworkedField]
    public bool Locked;

    [DataField]
    public int Revision;

    /// <summary>
    /// Maximum number of ambitions a character may keep at once.
    /// </summary>
    [DataField]
    public int MaxAmbitions = 3;
}
