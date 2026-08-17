using Content.Shared.FixedPoint;

namespace Content.Medical.Common.Inkymed.Events;

[ByRefEvent]
public record struct MobThresholdGetWoundableIntegrityEvent
{
    public bool Handled;
    public FixedPoint2 Damage;
}
