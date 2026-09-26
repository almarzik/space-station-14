// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
// Ported from CrystallEdge, TheShuEd and contributors, revision 728cd277da. Original authorship retained; SS220 notice covers adaptations.

using Content.Shared.Roles.Components;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Ambitions.Conditions;

[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class SS220AmbitionCondition
{
    [DataField]
    public bool Inverted = false;

    public abstract bool Check(IEntityManager entManager, IPrototypeManager protoManager, EntityUid owner);
}

public sealed partial class SS220AmbitionRequiredSpecies : SS220AmbitionCondition
{
    [DataField(required: true)]
    public HashSet<ProtoId<SpeciesPrototype>> Species = new();

    public override bool Check(IEntityManager entManager, IPrototypeManager protoManager, EntityUid owner)
    {
        if (!entManager.TryGetComponent(owner, out HumanoidProfileComponent? humanoid))
            return false;

        if (Inverted)
            return !Species.Contains(humanoid.Species);

        return Species.Contains(humanoid.Species);
    }
}

public sealed partial class SS220AmbitionRequiredJob : SS220AmbitionCondition
{
    [DataField(required: true)]
    public HashSet<ProtoId<JobPrototype>> Jobs = new();

    public override bool Check(IEntityManager entManager, IPrototypeManager protoManager, EntityUid owner)
    {
        var mindSystem = entManager.System<SharedMindSystem>();
        var jobSystem = entManager.System<SharedJobSystem>();

        if (!mindSystem.TryGetMind(owner, out var mindId, out _))
            return false;

        if (!jobSystem.MindTryGetJob(mindId, out var jobProto))
            return false;

        if (Inverted)
            return !Jobs.Contains(jobProto);

        return Jobs.Contains(jobProto);
    }
}

public sealed partial class SS220AmbitionRequiredDepartment : SS220AmbitionCondition
{
    [DataField(required: true)]
    public HashSet<ProtoId<DepartmentPrototype>> Departments = new();

    public override bool Check(IEntityManager entManager, IPrototypeManager protoManager, EntityUid owner)
    {
        var mindSystem = entManager.System<SharedMindSystem>();
        var jobSystem = entManager.System<SharedJobSystem>();

        if (!mindSystem.TryGetMind(owner, out var mindId, out var mind))
            return false;

        if (!jobSystem.MindTryGetJob(mindId, out var jobProto))
            return false;

        var matched = false;

        foreach (var deptId in Departments)
        {
            if (!protoManager.Resolve(deptId, out var department))
                continue;

            if (!department.Roles.Contains(jobProto))
                continue;

            matched = true;
            break;
        }

        if (Inverted)
            return !matched;

        return matched;
    }
}

public sealed partial class SS220AmbitionRequiredThiefRole : SS220AmbitionCondition
{
    public override bool Check(IEntityManager entManager, IPrototypeManager protoManager, EntityUid owner)
    {
        var mindSystem = entManager.System<SharedMindSystem>();

        if (!mindSystem.TryGetMind(owner, out var mindId, out var mind))
            return false;

        var matched = false;

        foreach (var roleId in mind.MindRoleContainer.ContainedEntities)
        {
            if (entManager.HasComponent<ThiefRoleComponent>(roleId))
            {
                matched = true;
                break;
            }
        }

        if (Inverted)
            return !matched;

        return matched;
    }
}
