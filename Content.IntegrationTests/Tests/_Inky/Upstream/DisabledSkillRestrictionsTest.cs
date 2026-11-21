// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable
using Content.Factory.Common.Construction;
using Content.Goobstation.Common.Weapons.Ranged;
using Content.Goobstation.Shared.Enchanting.Components;
using Content.Goobstation.Shared.Enchanting.Systems;
using Content.Shared.Construction;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.CCVar;
using Content.Trauma.Common.Chemistry;
using Content.Trauma.Common.Construction;
using Content.Trauma.Shared.Body.Chips;
using Content.Trauma.Shared.Knowledge.Quality;
using Content.Trauma.Shared.Knowledge.Systems;
using Content.Trauma.Shared.Weapons.Classes;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Inky.Upstream;

[EnsureCVar(Side.Server, typeof(TraumaCVars), nameof(TraumaCVars.SkillsEnabled), false)]
public sealed class DisabledSkillRestrictionsTest : GameTest
{
    private static readonly EntProtoId FirstAid = "FirstAidKnowledge";
    private static readonly EntProtoId<OrganChipComponent> SurgeonGeneralChip = "SkillChipCMO";
    private static readonly EntProtoId<EnchantComponent> Fortune = "EnchantFortune";

    [SidedDependency(Side.Server)] private readonly SharedConstructionSystem _construction = default!;
    [SidedDependency(Side.Server)] private readonly SharedKnowledgeSystem _knowledge = default!;
    [SidedDependency(Side.Server)] private readonly OrganChipSystem _organChips = default!;
    [SidedDependency(Side.Server)] private readonly EnchantingSystem _enchanting = default!;
    [SidedDependency(Side.Server)] private readonly EnchanterSystem _enchanters = default!;

    private EntityCoordinates _spawnCoordinates;

    [SetUp]
    public async Task SetUp()
    {
        var map = await Pair.CreateTestMap();
        _spawnCoordinates = map.GridCoords;
    }

    private EntityUid SpawnUntrainedUser()
    {
        var user = SSpawnAtPosition(null, _spawnCoordinates);
        // Give the user an empty knowledge container so it still goes through skill checks.
        _knowledge.EnsureKnowledgeContainer(user);
        return user;
    }

    [Test]
    public async Task ConstructionAndCraftingResultsStayUnchanged()
    {
        var usesKnowledge = false;
        var blockedRecipes = new List<string>();
        var recipesWithAddedQuality = new List<string>();

        await Server.WaitAssertion(() =>
        {
            var user = SpawnUntrainedUser();
            usesKnowledge = _construction.UsesKnowledge(user);

            foreach (var recipe in SProtoMan.EnumeratePrototypes<ConstructionPrototype>())
            {
                var attempt = new ConstructAttemptEvent(recipe.ID, LogError: false);
                SEntMan.EventBus.RaiseLocalEvent(user, ref attempt);
                if (attempt.Cancelled)
                    blockedRecipes.Add(recipe.ID);

                // Use a result with no inherent quality to isolate changes made by construction.
                var result = SSpawnAtPosition(null, _spawnCoordinates);
                var constructed = new ConstructedEvent(result, recipe.ID);
                SEntMan.EventBus.RaiseLocalEvent(user, ref constructed);
                if (SHasComp<QualityComponent>(result))
                    recipesWithAddedQuality.Add(recipe.ID);

                SDeleteNow(result);
            }
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(usesKnowledge, Is.False);
            Assert.That(blockedRecipes, Is.Empty, "The following recipes were blocked by skill checks.");
            Assert.That(recipesWithAddedQuality, Is.Empty, "The following recipes added quality with skills disabled.");
        }
    }

    [Test]
    public async Task WeaponClassesPreserveBaseDamageAndRecoil()
    {
        var modifiers = new Dictionary<ProtoId<WeaponClassPrototype>, (DamageSpecifier Damage, float Recoil)>();
        var baseDamage = new DamageSpecifier { DamageDict = { ["Blunt"] = 10 } };

        await Server.WaitAssertion(() =>
        {
            var user = SpawnUntrainedUser();
            var factory = SEntMan.ComponentFactory;
            var componentName = factory.GetComponentName<WeaponClassComponent>();

            foreach (var prototype in SProtoMan.EnumeratePrototypes<EntityPrototype>())
            {
                if (!prototype.Components.TryGetValue(componentName, out var entry)
                    || entry.Component is not WeaponClassComponent weaponClass
                    || modifiers.ContainsKey(weaponClass.Class))
                    continue;

                // Copy only the weapon class to isolate its modifiers from wielding and upgrades.
                var weapon = SSpawnAtPosition(null, _spawnCoordinates);
                SEntMan.AddComponent(weapon, factory.GetComponent(entry));
                var melee = new GetMeleeDamageEvent(weapon, new DamageSpecifier(baseDamage), [], user);
                SEntMan.EventBus.RaiseLocalEvent(weapon, ref melee);

                var recoil = new GetRecoilModifiersEvent(weapon, user);
                SEntMan.EventBus.RaiseLocalEvent(weapon, ref recoil);
                modifiers.Add(weaponClass.Class, (melee.Damage, recoil.Modifier));
            }
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(modifiers, Is.Not.Empty, "Expected at least one weapon class on a loaded entity prototype.");
            foreach (var (weaponClass, result) in modifiers)
            {
                Assert.That(result.Damage, Is.EqualTo(baseDamage), $"{weaponClass} melee damage");
                Assert.That(result.Recoil, Is.EqualTo(1f), $"{weaponClass} recoil");
            }
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task HumanInjectsAtBaseSpeed(bool installSkillChip)
    {
        var hasKnowledgeContainer = false;
        var hasFirstAid = false;
        int? installedChips = null;
        var baseDelay = TimeSpan.FromSeconds(2);
        var injectionDelay = TimeSpan.Zero;

        await Server.WaitAssertion(() =>
        {
            var human = SSpawnAtPosition("MobHuman", _spawnCoordinates);
            if (_knowledge.GetContainer(human) is { } brain)
            {
                hasKnowledgeContainer = true;
                if (installSkillChip)
                {
                    _organChips.InstallChip(human, SurgeonGeneralChip);
                    installedChips = SComp<OrganChipContainerComponent>(brain.Owner).Container.Count;
                }

                hasFirstAid = _knowledge.GetKnowledge(brain, FirstAid) != null;
            }

            var syringe = SSpawnAtPosition("Syringe", _spawnCoordinates);
            var inject = new UserModifyInjectTimeEvent(human, syringe, baseDelay);
            SEntMan.EventBus.RaiseLocalEvent(human, ref inject);
            injectionDelay = inject.Delay;
        });

        Assert.That(hasKnowledgeContainer, Is.True, "MobHuman must have a knowledge container in its brain.");
        using (Assert.EnterMultipleScope())
        {
            if (installSkillChip)
                Assert.That(installedChips, Is.EqualTo(1), $"Expected {SurgeonGeneralChip} in the brain.");

            Assert.That(hasFirstAid, Is.False, "First Aid is disabled.");
            Assert.That(injectionDelay, Is.EqualTo(baseDelay));
        }
    }

    [Test]
    public async Task UntrainedUserCanEnchantPickaxe()
    {
        var compatible = false;
        var succeeded = false;
        var hasFortune = false;

        await Server.WaitAssertion(() =>
        {
            var user = SpawnUntrainedUser();
            var diamond = SSpawnAtPosition("MaterialDiamond1", _spawnCoordinates);
            var pickaxe = SSpawnAtPosition("Pickaxe", _spawnCoordinates);
            compatible = _enchanting.CanEnchant(pickaxe, Fortune);
            if (!compatible)
                return;

            succeeded = _enchanters.TryEnchant(SEntity<EnchanterComponent>(diamond), pickaxe, user);
            if (STryComp<EnchantedComponent>(pickaxe, out var enchanted))
                hasFortune = _enchanting.FindEnchant(enchanted, Fortune) != null;
        });

        Assert.That(compatible, Is.True, "Pickaxe must accept Fortune.");
        Assert.That(succeeded, Is.True, "A compatible diamond and pickaxe must work without Magical Literacy.");
        Assert.That(hasFortune, Is.True, "The enchanted pickaxe must have Fortune.");
    }
}
