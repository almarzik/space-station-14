// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
#nullable enable

using Content.IntegrationTests.Fixtures;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Shared.Roles;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Localization;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.SS220.Ambitions;

public sealed class AmbitionsRoundEndTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = false };

    [TestCase(null, false)]
    [TestCase(null, true)]
    [TestCase("MindRoleTraitor", false)]
    [TestCase("MindRoleTraitor", true)]
    [TestCase("MindRoleThief", false)]
    [TestCase("MindRoleThief", true)]
    public async Task SummaryOnlyShowsAntagonistAmbitions(string? role, bool otherObjective)
    {
        await Server.WaitAssertion(() =>
        {
            var configuration = Server.ResolveDependency<IConfigurationManager>();
            var showGreentext = configuration.GetCVar(CCVars.GameShowGreentext);
            configuration.SetCVar(CCVars.GameShowGreentext, true);
            var mindSystem = Server.System<MindSystem>();
            var objectives = Server.System<ObjectivesSystem>();
            var owner = SEntMan.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
            var mind = mindSystem.CreateMind(null, "Ambition summary test character");
            mindSystem.TransferTo(mind, owner);
            if (role != null)
                Server.System<SharedRoleSystem>().MindAddRole(mind, role, silent: true);

            Assert.That(mindSystem.TryAddObjective(mind, mind.Comp, "SS220AmbitionWordOfHonor", out var ambition), Is.True);
            var ambitionTitle = objectives.GetInfo(ambition!.Value, mind, mind.Comp)!.Value.Title;
            string? otherTitle = null;
            if (otherObjective)
            {
                Assert.That(mindSystem.TryAddObjective(mind, mind.Comp, "EscapeShuttleObjective", out var other, force: true), Is.True);
                otherTitle = objectives.GetInfo(other!.Value, mind, mind.Comp)!.Value.Title;
            }

            var ev = new RoundEndTextAppendEvent();
            SEntMan.EventBus.RaiseEvent(EventSource.Local, ev);
            if (role == null)
                Assert.That(ev.Text, Does.Not.Contain(ambitionTitle));
            else
                Assert.That(ev.Text.Split(System.Environment.NewLine), Does.Contain("- " + ambitionTitle));

            if (otherTitle != null)
            {
                var localization = Server.ResolveDependency<ILocalizationManager>();
                var failure = localization.GetString("objectives-objective-fail", ("objective", otherTitle), ("progress", 0f));
                Assert.That(ev.Text, Does.Contain(failure));
            }
            else if (role == null)
                Assert.That(ev.Text, Does.Not.Contain(objectives.GetTitle(mind, mind.Comp.CharacterName!)));

            Assert.That(mind.Comp.Objectives, Has.Count.EqualTo(otherObjective ? 2 : 1));
            while (mind.Comp.Objectives.Count > 0)
                mindSystem.TryRemoveObjective(mind, mind.Comp, 0);
            SEntMan.DeleteEntity(owner);
            SEntMan.DeleteEntity(mind);
            configuration.SetCVar(CCVars.GameShowGreentext, showGreentext);
        });
    }
}
