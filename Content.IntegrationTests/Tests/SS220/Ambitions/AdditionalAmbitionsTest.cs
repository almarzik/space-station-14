// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.IntegrationTests.Fixtures;
using Content.Server.Mind;
using Content.Shared.Roles;
using Content.Shared.SS220.Ambitions.Components;
using Content.Shared.SS220.Ambitions.Conditions;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.SS220.Ambitions;

public sealed class AdditionalAmbitionsTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = false };

    [Test]
    public async Task TraitorAmbitionsRequireTraitorRole()
    {
        await Server.WaitAssertion(() =>
        {
            var owner = SEntMan.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
            var mindSystem = Server.System<MindSystem>();
            var roles = Server.System<SharedRoleSystem>();
            var condition = new SS220AmbitionRequiredTraitorRole();
            Assert.That(condition.Check(SEntMan, SProtoMan, owner), Is.False);
            var mind = mindSystem.CreateMind(null);
            mindSystem.TransferTo(mind, owner);
            Assert.That(condition.Check(SEntMan, SProtoMan, owner), Is.False);
            roles.MindAddRole(mind, "MindRoleThief", silent: true);
            Assert.That(condition.Check(SEntMan, SProtoMan, owner), Is.False);
            roles.MindAddRole(mind, "MindRoleTraitor", silent: true);
            Assert.That(condition.Check(SEntMan, SProtoMan, owner), Is.True);
            condition.Inverted = true;
            Assert.That(condition.Check(SEntMan, SProtoMan, owner), Is.False);
            SEntMan.DeleteEntity(owner);
            SEntMan.DeleteEntity(mind);
        });
    }

    [TestCase("SS220AmbitionFestival", "Captain", "Passenger")]
    [TestCase("SS220AmbitionPills", "Chemist", "MedicalDoctor")]
    [TestCase("SS220AmbitionPsychotrauma", "Psychologist", "Chemist")]
    [TestCase("SS220AmbitionBrigPsychologist", "Brigmedic", "SecurityOfficer")]
    [TestCase("SS220AmbitionHarshCommander", "ChiefEngineer", "StationEngineer")]
    [TestCase("SS220AmbitionRenovation", "StationEngineer", "Passenger")]
    [TestCase("SS220AmbitionBardSoul", "Musician", "Passenger")]
    [TestCase("SS220AmbitionBigBoom", "Clown", "Passenger")]
    public async Task RestrictedAmbitionsFollowJobAndDepartment(string objectiveId, string allowedJob, string deniedJob)
    {
        await Server.WaitAssertion(() =>
        {
            var owner = SEntMan.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
            var mindSystem = Server.System<MindSystem>();
            var roles = Server.System<SharedRoleSystem>();
            var mind = mindSystem.CreateMind(null);
            mindSystem.TransferTo(mind, owner);
            var prototype = SProtoMan.Index<EntityPrototype>(objectiveId);
            Assert.That(prototype.TryGetComponent<SS220AmbitionObjectiveComponent>(out var ambition, SEntMan.ComponentFactory), Is.True);
            Assert.That(ambition!.Conditions, Is.Not.Empty);
            roles.MindAddJobRole(mind, silent: true, jobPrototype: allowedJob);
            foreach (var condition in ambition.Conditions)
                Assert.That(condition.Check(SEntMan, SProtoMan, owner), Is.True, objectiveId);
            roles.MindAddJobRole(mind, silent: true, jobPrototype: deniedJob);
            var denied = false;
            foreach (var condition in ambition.Conditions)
                denied |= !condition.Check(SEntMan, SProtoMan, owner);
            Assert.That(denied, Is.True, objectiveId);
            SEntMan.DeleteEntity(owner);
            SEntMan.DeleteEntity(mind);
        });
    }
}
