using Content.Medical.Shared.Body;
using Content.Shared.Body;
using Content.Shared.Medical;
using Content.Shared.Mobs.Components;

namespace Content.Medical.Server.Inkymed.Systems;

public sealed partial class DefibrillatorHeartSystem : EntitySystem // slop
{
    [Dependency] private HeartSystem _heart = default!;
    [Dependency] private BodySystem _body = default!;

    public static readonly ProtoId<OrganCategoryPrototype> HeartCategory = "Heart";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodyComponent, TargetDefibrillatedEvent>(OnFibbed);
    }

    private void OnFibbed(EntityUid target, BodyComponent _, ref TargetDefibrillatedEvent args)
    {
        var defib = args.Defibrillator.Comp;

        if (defib.BpmZapHeal == 0)
            return;

        var heartUid = _body.GetOrgan(target, HeartCategory);
        if (heartUid == null
            || !TryComp<HeartComponent>(heartUid, out var heart))
            return;

        var baseHeal = heart.CurrentlyActive
            ? defib.BpmZapHeal
            : defib.BpmZapHealFlatline;
        if (baseHeal == 0)
            return;

        int delta;
        if (defib.AutoStabilisation)
        {
            var towardTarget = heart.CurrentHeartRate < heart.StartingHeartRate
                ? 1
                : -1;
            var sign = baseHeal >= 0
                ? towardTarget
                : -towardTarget;
            delta = Math.Abs(baseHeal) * sign;
        }
        else delta = baseHeal;

        _heart.SetHeartRate(heartUid.Value, heart, heart.CurrentHeartRate + delta, forced: true);
    }
}
