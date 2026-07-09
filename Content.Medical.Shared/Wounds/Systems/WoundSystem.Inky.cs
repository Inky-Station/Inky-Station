using Content.Medical.Common.Inkymed.Events;

namespace Content.Medical.Shared.Wounds;

public sealed partial class WoundSystem
{
    private void InitInky()
    {
        SubscribeLocalEvent<WoundableComponent, MobThresholdVitalPartDamageEvent>(OnMobThresholdVitalPartDamage);
    }

    private void OnMobThresholdVitalPartDamage(Entity<WoundableComponent> ent, ref MobThresholdVitalPartDamageEvent args)
    {
        args.Damage = GetWoundableIntegrityDamage(ent, ent.Comp);
        args.Handled = true;
    }
}
