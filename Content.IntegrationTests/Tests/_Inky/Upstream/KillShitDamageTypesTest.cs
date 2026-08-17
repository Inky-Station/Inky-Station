using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;

namespace Content.IntegrationTests.Tests._Inky.Upstream;

public sealed partial class KillShitDamageTypesTest : GameTest // israelgpt x palantirgpt bravest soldier wasted 65 gallons of watter for this, be greatful.
{
    private static readonly List<string> BannedDamageType = ["Ballistic"];

    private static readonly BindingFlags MemberFlags = // NO IDEA WHAT THIS IS
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    [Test]
    public async Task NoBallisticDamageInComponentsOrSystems()
    {
        var server = Pair.Server;
        var compFactory = server.ResolveDependency<IComponentFactory>();
        var reflection = server.ResolveDependency<IReflectionManager>();
        var entSysMan = server.ResolveDependency<IEntitySystemManager>();

        await server.WaitAssertion(() =>
        {
            var fail = new List<string>();

            foreach (var compType in compFactory.AllRegisteredTypes)
            {
                object instance;
                try
                {
                    instance = compFactory.GetComponent(compType);
                }
                catch
                {
                    continue;
                }

                InspectInstance(instance, compType, "Component", fail); // holy goida
            }

            var getEntitySystemGeneric = typeof(IEntitySystemManager)
                .GetMethod(nameof(IEntitySystemManager.GetEntitySystem), Type.EmptyTypes);

            if (getEntitySystemGeneric is null)
            {
                Assert.Fail("wow you fucked up the robusttoolbox congrats!");
                return;
            }

            foreach (var sysType in reflection.GetAllChildren<EntitySystem>())
            {
                if (sysType.IsAbstract)
                    continue;

                object sysInstance;
                try
                {
                    var method = getEntitySystemGeneric.MakeGenericMethod(sysType);
                    sysInstance = method.Invoke(entSysMan, null);
                }
                catch
                {
                    continue;
                }

                InspectInstance(sysInstance, sysType, "EntitySystem", fail); // goida
            }

            Assert.That(fail,
                Is.Empty,
                $"fucking '{string.Join(", ", BannedDamageType)}' exists in one of the systems! "
                + string.Join("\n", fail.Distinct().OrderBy(x => x))
            );
        });
    }

    private static void InspectInstance(object instance, Type ownerType, string kind, List<string> fail)
    {
        if (instance is null)
            return;

        var type = instance.GetType();

        foreach (var field in type.GetFields(MemberFlags))
        {
            object value;
            try
            {
                value = field.GetValue(field.IsStatic ? null : instance);
            }
            catch
            {
                continue;
            }

            CheckMember(value, $"{kind} {ownerType.Name}.{field.Name}", fail);
        }

        foreach (var prop in type.GetProperties(MemberFlags))
        {
            if (prop.GetIndexParameters().Length > 0)
                continue;

            if (prop.GetMethod is null)
                continue;

            object value;
            try
            {
                value = prop.GetValue(prop.GetMethod.IsStatic ? null : instance);
            }
            catch
            {
                continue;
            }

            CheckMember(value, $"{kind} {ownerType.Name}.{prop.Name}", fail);
        }
    }

    private static void CheckMember(object value, string location, List<string> fail)
    {
        switch (value)
        {
            case DamageSpecifier dmg:
                foreach (var damageType in BannedDamageType)
                {
                    if (dmg.DamageDict.ContainsKey(damageType))
                        fail.Add($"{location} shat itself with '{damageType}'");
                }
                break;

            case DamageModifierSet mod:
                foreach (var damageType in BannedDamageType)
                {
                    if (mod.Coefficients.ContainsKey(damageType))
                        fail.Add($"{location} shat itself with '{damageType}'");

                    if (mod.FlatReduction.ContainsKey(damageType))
                        fail.Add($"{location} shat itself with '{damageType}'");
                }
                break;
        }
    }
}
