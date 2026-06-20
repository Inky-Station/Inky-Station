using System.Collections.Generic;
using Content.IntegrationTests;
using Content.IntegrationTests.Fixtures;
using Content.Medical.Shared.Body;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Inky.Inkymed;

public sealed class OrganCategorySlotTest : GameTest // todo inkymed: nuke it asap
{
    /// <summary>
    /// ensures that no BodyPartComponent proto ever has its own category in its slots
    /// if wizden or someone ever does something similar to this, delete this (no one will) <--- :clueless:
    /// </summary>
    [Test]
    public async Task OrganCategoryCheckSlotTest()
    {
        var server = Pair.Server;

        var protoMan = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            var fail = new List<string>();

            foreach (var proto in protoMan.EnumeratePrototypes<EntityPrototype>())
            {
                if (!proto.TryGetComponent<BodyPartComponent>(out var bodyPart))
                    continue;

                if (!proto.TryGetComponent<OrganComponent>(out var organ))
                    continue;

                if (organ.Category is not { } ownCategory)
                    continue;

                if (!bodyPart.Slots.Contains(ownCategory))
                    continue;

                fail.Add($"'{proto.ID}' listst its own category '{ownCategory}' in BodyPartComponent.Slots"
                );
            }

            Assert.That(fail, Is.Empty,
                "one or more body parts contain their own OrganCategory in their slots set.\n"
                + string.Join("\n", fail)
            );
        });
    }
}
