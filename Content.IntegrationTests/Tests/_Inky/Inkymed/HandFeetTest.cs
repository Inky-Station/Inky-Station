using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Medical.Common.Body;
using Content.Shared.Armor;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Inky.Inkymed;

public sealed class HandFeetTest : GameTest
{
    /// <summary>
    /// makes sure that no piece of armor has bad bodypart coverage
    /// </summary>
    [Test]
    public async Task ArmorCoverageCheckTest()
    {
        var server = Pair.Server;
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var shitParts = new[]
        {
            BodyPartType.Foot,
            BodyPartType.Hand,
        };

        await server.WaitAssertion(() =>
        {
            var fail = new List<string>();

            foreach (var proto in protoMan.EnumeratePrototypes<EntityPrototype>())
            {
                if (!proto.TryGetComponent<ArmorComponent>(out var armor))
                    continue;

                var found = armor.ArmorCoverage
                    .Where(part => shitParts.Contains(part))
                    .ToList();

                if (found.Count == 0)
                    continue;

                var listed = string.Join(", ", found);
                fail.Add($"'{proto.ID}' has fucked coverage: [{listed}]");
            }

            Assert.That(fail, Is.Empty,
                "one or more armor prototypes cover Foot or Hand\n"
                + string.Join("\n", fail)
            );
        });
    }

    [Test]
    public async Task BadOrgansCheckTest()
    {
        var server = Pair.Server;
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var shitSlots = new HashSet<string>
        {
            "FootLeft",
            "FootRight",
            "HandLeft",
            "HandRight",
        };

        await server.WaitAssertion(() =>
        {
            var fail = new List<string>();

            foreach (var proto in protoMan.EnumeratePrototypes<EntityPrototype>())
            {
                if (!proto.TryGetComponent<InitialBodyComponent>(out var initialBody))
                    continue;

                var offending = initialBody.Organs.Keys
                    .Where(slot => shitSlots.Contains(slot))
                    .ToList();

                if (offending.Count == 0)
                    continue;

                var listed = string.Join(", ", offending);
                fail.Add($"'{proto.ID}' has obsolete organ slots: [{listed}]");
            }

            Assert.That(fail, Is.Empty,
                "one or more InitialBodyComponent protos define FootLeft, FootRight, HandLeft, or HandRight organ slots\n"
                + string.Join("\n", fail)
            );
        });
    }
}
