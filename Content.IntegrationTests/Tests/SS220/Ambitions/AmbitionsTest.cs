// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
#nullable enable
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Robust.Server.GameObjects;
using Content.Client.SS220.Ambitions;
using Content.IntegrationTests.Fixtures;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Localization;
using Content.Server.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.SS220.Ambitions;
using Content.Shared.SS220.Ambitions.Components;
using Content.Shared.SS220.Ambitions.Parsings;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.IntegrationTests.Tests.SS220.Ambitions;

public sealed partial class AmbitionsTest : GameTest
{
    [GeneratedRegex(@"!([A-Z0-9]+)!")]
    private static partial Regex PlaceholderRegex();

    public override PoolSettings PoolSettings => new() { Connected = false };

    [Test]
    public async Task OwnershipBudgetDeletionAndLock()
    {
        await Server.WaitAssertion(() =>
        {
            var mindSystem = Server.System<MindSystem>();
            var owner = SEntMan.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
            var stranger = SEntMan.SpawnEntity(null, MapCoordinates.Nullspace);
            var setup = SEntMan.GetComponent<SS220AmbitionsSetupComponent>(owner);
            var mind = mindSystem.CreateMind(null);
            mindSystem.TransferTo(mind, owner);
            var ui = Server.System<UserInterfaceSystem>();
            Assert.That(ui.TryOpenUi(owner, SS220AmbitionsUIKey.Key, stranger), Is.False);
            Assert.That(ui.TryOpenUi(owner, SS220AmbitionsUIKey.Key, owner), Is.True);

            var unauthorized = new SS220AmbitionCreateMessage(0) { Actor = stranger };
            SEntMan.EventBus.RaiseLocalEvent(owner, unauthorized);
            Assert.That(mind.Comp.Objectives, Is.Empty);
            Assert.That(setup.RerollAmount, Is.EqualTo(10));

            for (var i = 0; i < 3; i++)
            {
                var create = new SS220AmbitionCreateMessage(setup.Revision) { Actor = owner };
                SEntMan.EventBus.RaiseLocalEvent(owner, create);
            }

            Assert.That(mind.Comp.Objectives, Has.Count.EqualTo(3));
            Assert.That(setup.RerollAmount, Is.EqualTo(7));
            var overLimit = new SS220AmbitionCreateMessage(setup.Revision) { Actor = owner };
            SEntMan.EventBus.RaiseLocalEvent(owner, overLimit);
            Assert.That(mind.Comp.Objectives, Has.Count.EqualTo(3));
            Assert.That(setup.RerollAmount, Is.EqualTo(7));

            var objective = mind.Comp.Objectives[0];
            EntProtoId prototype = SEntMan.GetComponent<MetaDataComponent>(objective).EntityPrototype!.ID;
            var stale = new SS220AmbitionDeleteMessage(prototype, 0) { Actor = owner };
            SEntMan.EventBus.RaiseLocalEvent(owner, stale);
            Assert.That(mind.Comp.Objectives, Has.Count.EqualTo(3));

            var delete = new SS220AmbitionDeleteMessage(prototype, setup.Revision) { Actor = owner };
            SEntMan.EventBus.RaiseLocalEvent(owner, delete);
            Assert.That(mind.Comp.Objectives, Has.Count.EqualTo(2));
            Assert.That(setup.RerollAmount, Is.EqualTo(7), "Deleting must not refund attempts.");

            // Spend the rest of the budget, deleting one ambition each time to make room.
            while (setup.RerollAmount > 0)
            {
                var create = new SS220AmbitionCreateMessage(setup.Revision) { Actor = owner };
                SEntMan.EventBus.RaiseLocalEvent(owner, create);
                Assert.That(mind.Comp.Objectives, Has.Count.EqualTo(3));
                var first = mind.Comp.Objectives[0];
                EntProtoId id = SEntMan.GetComponent<MetaDataComponent>(first).EntityPrototype!.ID;
                var remove = new SS220AmbitionDeleteMessage(id, setup.Revision) { Actor = owner };
                SEntMan.EventBus.RaiseLocalEvent(owner, remove);
            }

            var exhausted = new SS220AmbitionCreateMessage(setup.Revision) { Actor = owner };
            SEntMan.EventBus.RaiseLocalEvent(owner, exhausted);
            Assert.That(mind.Comp.Objectives, Has.Count.EqualTo(2));
            Assert.That(setup.RerollAmount, Is.Zero);

            var lockMessage = new SS220AmbitionLockMessage(setup.Revision) { Actor = owner };
            SEntMan.EventBus.RaiseLocalEvent(owner, lockMessage);
            Assert.That(setup.Locked, Is.True);
            EntProtoId lockedId = SEntMan.GetComponent<MetaDataComponent>(mind.Comp.Objectives[0]).EntityPrototype!.ID;
            var lockedDelete = new SS220AmbitionDeleteMessage(lockedId, setup.Revision) { Actor = owner };
            SEntMan.EventBus.RaiseLocalEvent(owner, lockedDelete);
            Assert.That(mind.Comp.Objectives, Has.Count.EqualTo(2));

            while (mind.Comp.Objectives.Count > 0)
                mindSystem.TryRemoveObjective(mind, mind.Comp, 0);
            SEntMan.DeleteEntity(owner);
            SEntMan.DeleteEntity(stranger);
            SEntMan.DeleteEntity(mind);
        });
    }

    [Test]
    public async Task EmptyEntityPoolDoesNotThrow()
    {
        await Server.WaitAssertion(() =>
        {
            var parsing = new SS220AmbitionRandomEntity { Whitelist = new() { "NotARealComponent" } };
            var random = Server.ResolveDependency<IRobustRandom>();
            Assert.That(parsing.GetText(SEntMan, SProtoMan, random, null), Is.Null);
        });
    }

    [Test]
    public async Task TemplatesAreLocalized()
    {
        await Server.WaitAssertion(() =>
        {
            var localization = Server.ResolveDependency<ILocalizationManager>();
            foreach (var prototype in SProtoMan.EnumeratePrototypes<EntityPrototype>())
            {
                if (prototype.Abstract || !prototype.TryGetComponent<SS220AmbitionObjectiveComponent>(out var ambition, SEntMan.ComponentFactory))
                    continue;

                Assert.That(localization.HasString(ambition.Name), Is.True, prototype.ID);
                Assert.That(localization.HasString(ambition.Desc), Is.True, prototype.ID);
                Assert.That(ambition.Weight, Is.GreaterThan(0), prototype.ID);
                var text = localization.GetString(ambition.Name) + localization.GetString(ambition.Desc);
                foreach (System.Text.RegularExpressions.Match match in PlaceholderRegex().Matches(text))
                    Assert.That(ambition.Parsings, Does.ContainKey(match.Groups[1].Value), prototype.ID);
            }
        });
    }

    [Test]
    public async Task MenuLoadsAndReflectsBudget()
    {
        await Client.WaitAssertion(() =>
        {
            var menu = new SS220AmbitionsMenu();
            Assert.That(menu.AddAmbitionButton.Disabled, Is.True);
            var entries = new List<SS220AmbitionInfo>
            {
                new("SS220AmbitionWordOfHonor", "Title [literal]", "Description"),
            };
            menu.Update(new SS220AmbitionsBuiState(entries, 9, 3, 1));
            Assert.That(menu.AddAmbitionButton.Disabled, Is.False);
            Assert.That(menu.FindControl<BoxContainer>("AmbitionsContainer").ChildCount, Is.EqualTo(1));
            menu.Update(new SS220AmbitionsBuiState(entries, 0, 3, 2));
            Assert.That(menu.AddAmbitionButton.Disabled, Is.True);
            Assert.That(menu.LockAmbitionButton.Disabled, Is.False);
            menu.Update(new SS220AmbitionsBuiState(new(), 1, 3, 3, "No candidates"));
            Assert.That(menu.FindControl<RichTextLabel>("ErrorLabel").Visible, Is.True);
            Assert.That(menu.FindControl<BoxContainer>("AmbitionsContainer").ChildCount, Is.Zero);
            menu.Close();
        });
    }
}
