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
        args.Damage = ent.Comp.IntegrityCap - ent.Comp.WoundableIntegrity;
        args.Handled = true;
    }
}
