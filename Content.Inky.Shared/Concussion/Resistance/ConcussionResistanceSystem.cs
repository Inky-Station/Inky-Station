using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;

namespace Content.Inky.Shared.Concussion.Resistance;

public sealed partial class ConcussionResistanceSystem : EntitySystem
{
    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
    private void OnExamine(EntityUid uid, ConcussionResistanceComponent comp, ref ExaminedEvent args)
    {
        if (comp.Resistance <= 0f)
            return;

        var percent = comp.Resistance * 100f;
        args.PushMarkup(Loc.GetString("concussion-resistance-percent", ("percent", percent)));
    }
}
