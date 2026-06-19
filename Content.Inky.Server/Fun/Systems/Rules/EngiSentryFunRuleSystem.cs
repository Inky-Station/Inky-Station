using Content.Inky.Server.Fun.Components;
using Content.Inky.Server.Fun.Components.Rules;
using Content.Server.GameTicking.Rules;
using Content.Shared.Damage.Systems;
using Content.Shared.Stacks;

namespace Content.Inky.Server.Fun.Systems.Rules;

public sealed class EngiSentryFunRuleSystem : GameRuleSystem<EngiSentryFunRuleComponent>
{
    [Dependency] private readonly FunnyThingsSystem _fun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnOnDamageComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SpawnOnDamageComponent, DamageChangedEvent>(OnDamaged);
    }

    private void OnInit(EntityUid uid, SpawnOnDamageComponent comp, ComponentInit args)
    {
        if (!_fun.CheckRule<EngiSentryFunRuleComponent>())
            return;
        comp.Active = true;
    }

    private void OnDamaged(EntityUid uid, SpawnOnDamageComponent comp, DamageChangedEvent args)
    {
        if (!comp.Active
            || args.DamageDelta == null
            || !args.DamageIncreased)
            return;

        if (comp.EngiOnly
            && (args.Origin == null || !HasComp<InkyEngineeringStaffComponent>(args.Origin.Value)))
            return;

        if (TryComp<StackComponent>(uid, out var stack) && stack.Count != comp.StackRequired)
            return;

        var xform = Transform(uid);
        Spawn(comp.Spawn, xform.Coordinates);
        QueueDel(uid);
    }
}
