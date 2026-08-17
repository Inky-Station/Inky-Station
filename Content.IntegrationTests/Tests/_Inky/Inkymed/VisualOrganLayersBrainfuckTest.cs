using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Inky.Inkymed;

public sealed class VisualOrganLayersBrainfuckTest : GameTest
{
    /// <summary>
    /// ensures that any visualorgancomponent with secondlayer has both
    /// an RSI path and a state on its seconddata
    /// </summary>
    [Test]
    public async Task SecondLayerDataCheckTest()
    {
        var server = Pair.Server;
        var protoMan = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            var fail = new List<string>();

            foreach (var proto in protoMan.EnumeratePrototypes<EntityPrototype>())
            {
                if (!proto.TryGetComponent<VisualOrganComponent>(out var visual))
                    continue;

                if (visual.SecondLayer == null)
                    continue;

                if (visual.SecondData == null)
                {
                    fail.Add($"'{proto.ID}' has SecondLayer '{visual.SecondLayer}' but SecondData is null");
                    continue;
                }

                if (visual.SecondData.RsiPath == null)
                    fail.Add($"'{proto.ID}' SecondData is missing RsiPath (state: '{visual.SecondData.State ?? "null"}')");

                if (visual.SecondData.State == null)
                    fail.Add($"'{proto.ID}' SecondData is missing State (rsi: '{visual.SecondData.RsiPath ?? "null"}')");
            }

            Assert.That(fail, Is.Empty,
                "One or more VisualOrganComponent protos have an incomplete SecondData.\n"
                + "This usually means that limbs/species you have changed are a piece of shit and you should be ashamed of yourself.\n"
                + string.Join("\n", fail)
            );
        });
    }
}
