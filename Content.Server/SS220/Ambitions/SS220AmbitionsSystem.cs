// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
// Adapted from CrystallEdge ambitions by TheShuEd and contributors (728cd277da).

using Content.Server.Mind;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.SS220.Ambitions;
using Content.Shared.SS220.Ambitions.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.Ambitions;

/// <summary>
/// Optional roleplay objectives. Generation, ownership and the attempt budget are server-authoritative.
/// </summary>
public sealed class SS220AmbitionsSystem : SS220SharedAmbitionsSystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    // Derived prototype data only; player state is stored on the setup component and mind.
    private readonly List<(EntProtoId Id, float Weight)> _ambitions = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SS220AmbitionsSetupComponent, SS220AmbitionCreateMessage>(OnCreate);
        SubscribeLocalEvent<SS220AmbitionsSetupComponent, SS220AmbitionDeleteMessage>(OnDelete);
        SubscribeLocalEvent<SS220AmbitionsSetupComponent, SS220AmbitionLockMessage>(OnLock);
        SubscribeLocalEvent<SS220AmbitionsSetupComponent, BoundUIOpenedEvent>(OnOpened);
        SubscribeLocalEvent<SS220AmbitionObjectiveComponent, ObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<SS220AmbitionObjectiveComponent, ObjectiveGetProgressEvent>(OnProgress);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnReload);
        SubscribeNetworkEvent<SS220ToggleAmbitionsScreenEvent>(OnToggle);
        CacheAmbitions();
    }

    private void OnReload(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<EntityPrototype>())
            CacheAmbitions();
    }

    private void CacheAmbitions()
    {
        _ambitions.Clear();
        foreach (var prototype in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (prototype.Abstract ||
                !prototype.Components.TryGetComponent<ObjectiveComponent>(_factory, out _) ||
                !prototype.Components.TryGetComponent<SS220AmbitionObjectiveComponent>(_factory, out var ambition) ||
                !float.IsFinite(ambition.Weight) || ambition.Weight <= 0)
                continue;

            _ambitions.Add((prototype.ID, ambition.Weight));
        }
    }

    private bool TryGetOwner(Entity<SS220AmbitionsSetupComponent> ent, EntityUid actor, out Entity<MindComponent> mind)
    {
        mind = default;
        if (actor != ent.Owner || TerminatingOrDeleted(ent) || ent.Comp.Locked ||
            !_mind.TryGetMind(ent, out var mindId, out var comp) || comp.OwnedEntity != ent.Owner)
            return false;

        mind = (mindId, comp);
        return true;
    }

    private void OnCreate(Entity<SS220AmbitionsSetupComponent> ent, ref SS220AmbitionCreateMessage args)
    {
        if (!TryGetOwner(ent, args.Actor, out var mind) || args.Revision != ent.Comp.Revision ||
            ent.Comp.RerollAmount <= 0)
            return;

        var count = 0;
        var existing = new HashSet<EntProtoId>();
        foreach (var objective in mind.Comp.Objectives)
        {
            if (TerminatingOrDeleted(objective) || !HasComp<SS220AmbitionObjectiveComponent>(objective))
                continue;

            count++;
            if (MetaData(objective).EntityPrototype is { } prototype)
                existing.Add(prototype.ID);
        }

        if (count >= ent.Comp.MaxAmbitions)
            return;

        var candidates = new List<(EntProtoId Id, float Weight)>();
        foreach (var candidate in _ambitions)
        {
            if (existing.Contains(candidate.Id))
                continue;

            var prototype = _prototypes.Index(candidate.Id);
            if (!prototype.Components.TryGetComponent<SS220AmbitionObjectiveComponent>(_factory, out var ambition))
                continue;

            var suitable = true;
            foreach (var condition in ambition.Conditions)
            {
                if (condition.Check(EntityManager, _prototypes, ent))
                    continue;

                suitable = false;
                break;
            }

            if (suitable)
                candidates.Add(candidate);
        }

        // Try every eligible candidate at most once. Missing players or content must not waste attempts.
        while (candidates.Count > 0)
        {
            var total = 0d;
            foreach (var candidate in candidates)
                total += candidate.Weight;

            var value = _random.NextFloat() * total;
            var index = candidates.Count - 1;
            for (var i = 0; i < candidates.Count; i++)
            {
                value -= candidates[i].Weight;
                if (value > 0)
                    continue;

                index = i;
                break;
            }

            var selected = candidates[index];
            candidates.RemoveAt(index);
            if (!_mind.TryAddObjective(mind, mind.Comp, selected.Id))
                continue;

            ent.Comp.RerollAmount--;
            ent.Comp.Revision++;
            Dirty(ent);
            UpdateUi(ent, mind);
            return;
        }

        UpdateUi(ent, mind, Loc.GetString("ss220-ambitions-no-candidates"));
    }

    private void OnDelete(Entity<SS220AmbitionsSetupComponent> ent, ref SS220AmbitionDeleteMessage args)
    {
        if (!TryGetOwner(ent, args.Actor, out var mind) || args.Revision != ent.Comp.Revision)
            return;

        foreach (var objective in mind.Comp.Objectives)
        {
            if (TerminatingOrDeleted(objective) || !HasComp<SS220AmbitionObjectiveComponent>(objective) ||
                MetaData(objective).EntityPrototype?.ID != args.Prototype.Id)
                continue;

            _mind.TryRemoveObjective(mind, mind.Comp, objective);
            ent.Comp.Revision++;
            UpdateUi(ent, mind);
            return;
        }
    }

    private void OnLock(Entity<SS220AmbitionsSetupComponent> ent, ref SS220AmbitionLockMessage args)
    {
        if (!TryGetOwner(ent, args.Actor, out _) || args.Revision != ent.Comp.Revision)
            return;

        ent.Comp.Locked = true;
        ent.Comp.Revision++;
        Dirty(ent);
        _ui.CloseUi(ent.Owner, SS220AmbitionsUIKey.Key);
    }

    private void OnAssigned(Entity<SS220AmbitionObjectiveComponent> ent, ref ObjectiveAssignedEvent args)
    {
        if (args.Mind.OwnedEntity is not { } owner)
        {
            args.Cancelled = true;
            return;
        }

        foreach (var condition in ent.Comp.Conditions)
        {
            if (condition.Check(EntityManager, _prototypes, owner))
                continue;

            args.Cancelled = true;
            return;
        }

        var title = Loc.GetString(ent.Comp.Name);
        var description = Loc.GetString(ent.Comp.Desc);
        foreach (var (key, parsing) in ent.Comp.Parsings)
        {
            var text = parsing.GetText(EntityManager, _prototypes, _random, owner);
            if (text == null)
            {
                args.Cancelled = true;
                return;
            }

            // Use the same draw in both fields; do not generate a second value after validation.
            title = title.Replace($"!{key}!", text);
            description = description.Replace($"!{key}!", text);
        }

        _metadata.SetEntityName(ent, title);
        _metadata.SetEntityDescription(ent, description);
    }

    private void OnProgress(Entity<SS220AmbitionObjectiveComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        // Ambitions are roleplay prompts, not automatically scored antagonist objectives.
        args.Progress = 0f;
    }

    private void OnToggle(SS220ToggleAmbitionsScreenEvent message, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } actor ||
            !TryComp<SS220AmbitionsSetupComponent>(actor, out var setup) ||
            !TryGetOwner((actor, setup), actor, out _))
            return;

        _ui.TryToggleUi(actor, SS220AmbitionsUIKey.Key, args.SenderSession);
    }

    private void OnOpened(Entity<SS220AmbitionsSetupComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!args.UiKey.Equals(SS220AmbitionsUIKey.Key))
            return;

        if (!TryGetOwner(ent, args.Actor, out var mind))
        {
            _ui.CloseUi(ent.Owner, args.UiKey, args.Actor);
            return;
        }

        UpdateUi(ent, mind);
    }

    private void UpdateUi(Entity<SS220AmbitionsSetupComponent> ent, Entity<MindComponent> mind, string? error = null)
    {
        var objectives = new List<SS220AmbitionInfo>();
        foreach (var objective in mind.Comp.Objectives)
        {
            if (TerminatingOrDeleted(objective) || !HasComp<SS220AmbitionObjectiveComponent>(objective))
                continue;

            var metadata = MetaData(objective);
            if (metadata.EntityPrototype is { } prototype)
                objectives.Add(new SS220AmbitionInfo(prototype.ID, metadata.EntityName, metadata.EntityDescription));
        }

        _ui.SetUiState(ent.Owner, SS220AmbitionsUIKey.Key,
            new SS220AmbitionsBuiState(objectives, ent.Comp.RerollAmount, ent.Comp.MaxAmbitions, ent.Comp.Revision, error));
    }
}
