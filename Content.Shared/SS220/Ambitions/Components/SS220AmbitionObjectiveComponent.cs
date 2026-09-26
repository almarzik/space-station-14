// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
// Ported from CrystallEdge, TheShuEd and contributors, revision 728cd277da. Original authorship retained; SS220 notice covers adaptations.

using Content.Shared.SS220.Ambitions.Conditions;
using Content.Shared.SS220.Ambitions.Parsings;

namespace Content.Shared.SS220.Ambitions.Components;

/// <summary>
/// This component is used to mark the EntProtoId objective as available for procedural ambition generation
/// and subsequent configuration in the character.
/// </summary>
[RegisterComponent, Access(typeof(SS220SharedAmbitionsSystem))]
public sealed partial class SS220AmbitionObjectiveComponent : Component
{
    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public LocId Desc;

    [DataField]
    public Dictionary<string, SS220AmbitionParsing> Parsings = new();

    [DataField]
    public List<SS220AmbitionCondition> Conditions = new();

    [DataField]
    public float Weight = 1f;
}
