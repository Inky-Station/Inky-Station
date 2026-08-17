using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;

namespace Content.Inky.Shared.Concussion.Resistance;

public sealed class ConcussionResistanceSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ConcussionResistanceComponent, InventoryRelayedEvent<BeforeConcussionDamageEvent>>(OnBeforeDamage);
        SubscribeLocalEvent<ConcussionResistanceComponent, ExaminedEvent>(OnExamine);
    }

    private static void OnBeforeDamage(EntityUid uid, ConcussionResistanceComponent comp, ref InventoryRelayedEvent<BeforeConcussionDamageEvent> args)
    {
        var damage = args.Args.Damage;

        damage *= 1f - Math.Clamp(comp.Resistance, 0f, 1f);
        if (damage <= 0f)
        {
            args.Args.Cancelled = true;
            return;
        }

        args.Args.Damage = damage;
    }
    private void OnExamine(EntityUid uid, ConcussionResistanceComponent comp, ref ExaminedEvent args)
    {
        if (comp.Resistance <= 0f)
            return;

        var percent = comp.Resistance * 100f;
        args.PushMarkup(Loc.GetString("concussion-resistance-percent", ("percent", percent)));
    }
}
