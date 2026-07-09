using Content.Shared.FixedPoint;

namespace Content.Medical.Common.Inkymed.Events;

[ByRefEvent]
public record struct MobThresholdVitalPartDamageEvent(FixedPoint2 Damage, bool Handled = false);
