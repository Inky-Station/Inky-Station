using Content.Medical.Common.Inkymed.Events;

namespace Content.Medical.Shared.Wounds;

public sealed partial class WoundSystem
{
    private void InitInky()
    {
        SubscribeLocalEvent<WoundableComponent, MobThresholdGetWoundableIntegrityEvent>(OnMobThresholdGetWoundableIntegrityEvent);
    }

    private void OnMobThresholdGetWoundableIntegrityEvent(Entity<WoundableComponent> ent, ref MobThresholdGetWoundableIntegrityEvent args)
    {
        args.Damage = ent.Comp.IntegrityCap - ent.Comp.WoundableIntegrity;
        args.Handled = true;
    }
}
