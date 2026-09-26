// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Ambitions.Conditions;

public sealed partial class SS220AmbitionRequiredTraitorRole : SS220AmbitionCondition
{
    public override bool Check(IEntityManager entManager, IPrototypeManager protoManager, EntityUid owner)
    {
        var mindSystem = entManager.System<SharedMindSystem>();
        if (!mindSystem.TryGetMind(owner, out var mindId, out _))
            return false;

        var matched = entManager.System<SharedRoleSystem>().MindHasRole<TraitorRoleComponent>(mindId);
        return Inverted ? !matched : matched;
    }
}
