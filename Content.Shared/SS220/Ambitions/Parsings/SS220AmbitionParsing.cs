// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
// Ported from CrystallEdge, TheShuEd and contributors, revision 728cd277da. Original authorship retained; SS220 notice covers adaptations.

using Content.Shared.Dataset;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Pinpointer;
using Content.Shared.Research.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using JetBrains.Annotations;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.SS220.Ambitions.Parsings;

[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class SS220AmbitionParsing
{
    public abstract string? GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random, EntityUid? owner);
}

public sealed partial class SS220AmbitionRandomFood : SS220AmbitionParsing
{
    public override string? GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random, EntityUid? owner)
    {
        var foods = new List<EntityPrototype>();
        foreach (var prototype in protoManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (!prototype.Abstract && !prototype.HideSpawnMenu && prototype.Components.ContainsKey("Food"))
                foods.Add(prototype);
        }

        return foods.Count == 0 ? null : random.Pick(foods).Name;
    }
}

public sealed partial class SS220AmbitionRandomDataset : SS220AmbitionParsing
{
    [DataField(required: true)]
    public ProtoId<LocalizedDatasetPrototype> Dataset;

    public override string? GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random, EntityUid? owner)
    {
        if (!protoManager.Resolve(Dataset, out var resolvedDataset))
            return null;

        if (resolvedDataset.Values.Count == 0)
            return null;

        var value = random.Pick(resolvedDataset.Values);
        return Loc.GetString(value);
    }
}

public sealed partial class SS220AmbitionRandomEntity : SS220AmbitionParsing
{
    [DataField]
    public ProtoId<EntityCategoryPrototype>? Category;

    [DataField]
    public List<string> Whitelist = new();

    [DataField]
    public List<string> Blacklist = new();

    public override string? GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random, EntityUid? owner)
    {
        var all = new List<EntityPrototype>();
        EntityCategoryPrototype? filter = null;
        if (Category is { } category && !protoManager.TryIndex(category, out filter))
            return null;

        foreach (var item in protoManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (item.Abstract)
                continue;
            if (item.HideSpawnMenu)
                continue;
            if (filter != null && !item.Categories.Contains(filter))
                continue;
            var suitable = true;
            foreach (var compName in Whitelist)
            {
                if (!item.Components.TryGetComponent(compName, out _))
                {
                    suitable = false;
                    break;
                }
            }

            foreach (var compName in Blacklist)
            {
                if (item.Components.TryGetComponent(compName, out _))
                {
                    suitable = false;
                    break;
                }
            }

            if (!suitable)
                continue;

            all.Add(item);
        }

        return all.Count == 0 ? null : random.Pick(all).Name;
    }
}

public sealed partial class SS220AmbitionRandomNumber : SS220AmbitionParsing
{
    [DataField(required: true)]
    public MinMax Range;

    public override string? GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random, EntityUid? owner)
    {
        return Range.Next(random).ToString();
    }
}

public sealed partial class SS220AmbitionRandomOtherJob : SS220AmbitionParsing
{
    public override string? GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random, EntityUid? owner)
    {
        var all = new List<JobPrototype>();

        var mindSys = entManager.System<SharedMindSystem>();
        var jobSys = entManager.System<SharedJobSystem>();

        if (owner == null)
            return null;

        if (!mindSys.TryGetMind(owner.Value, out var mindId, out _))
            return null;

        if (!jobSys.MindTryGetJob(mindId, out var currentJob))
            return null;

        foreach (var job in protoManager.EnumeratePrototypes<JobPrototype>())
        {
            if (currentJob == job)
                continue;

            if (!job.SetPreference)
                continue;

            all.Add(job);
        }

        if (all.Count == 0)
            return null;

        var name = random.Pick(all).Name;
        return entManager.System<SS220SharedAmbitionsSystem>().LocalizePrototypeName(name);
    }
}

public sealed partial class SS220AmbitionRandomSpecies : SS220AmbitionParsing
{
    public override string? GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random, EntityUid? owner)
    {
        var all = new List<SpeciesPrototype>();

        foreach (var species in protoManager.EnumeratePrototypes<SpeciesPrototype>())
        {
            if (!species.RoundStart)
                continue;

            all.Add(species);
        }

        if (all.Count == 0)
            return null;

        return entManager.System<SS220SharedAmbitionsSystem>().LocalizePrototypeName(random.Pick(all).Name);
    }
}

public sealed partial class SS220AmbitionRandomLocation : SS220AmbitionParsing
{
    public override string? GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random, EntityUid? owner)
    {
        if (owner == null || !entManager.TryGetComponent(owner.Value, out TransformComponent? transform) ||
            transform.GridUid is not { } grid)
            return null;

        var locations = new List<string>();
        var query = entManager.EntityQueryEnumerator<NavMapBeaconComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var beacon, out var beaconTransform))
        {
            if (!beacon.Enabled || beaconTransform.GridUid != grid)
                continue;

            var name = beacon.Text ?? entManager.GetComponent<MetaDataComponent>(uid).EntityName;
            if (!string.IsNullOrWhiteSpace(name) && !locations.Contains(name))
                locations.Add(name);
        }

        return locations.Count == 0 ? null : random.Pick(locations);
    }
}

public sealed partial class SS220AmbitionRandomScienceDiscovery : SS220AmbitionParsing
{
    public override string? GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random, EntityUid? owner)
    {
        var technologies = new List<TechnologyPrototype>();
        foreach (var technology in protoManager.EnumeratePrototypes<TechnologyPrototype>())
        {
            if (!technology.Hidden)
                technologies.Add(technology);
        }

        return technologies.Count == 0 ? null : Loc.GetString(random.Pick(technologies).Name);
    }
}

public sealed partial class SS220AmbitionRandomOtherPerson : SS220AmbitionParsing
{
    public override string? GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random, EntityUid? owner)
    {
        if (owner == null || !entManager.TryGetComponent(owner.Value, out TransformComponent? ownerTransform))
            return null;

        var names = new List<string>();
        foreach (var player in Filter.GetAllPlayers())
        {
            if (player.AttachedEntity is not { } entity || entity == owner ||
                entManager.HasComponent<GhostComponent>(entity) ||
                !entManager.HasComponent<HumanoidProfileComponent>(entity) ||
                !entManager.TryGetComponent(entity, out TransformComponent? transform) ||
                transform.MapID != ownerTransform.MapID)
                continue;

            names.Add(Identity.Name(entity, entManager, owner));
        }

        return names.Count == 0 ? null : random.Pick(names);
    }
}
