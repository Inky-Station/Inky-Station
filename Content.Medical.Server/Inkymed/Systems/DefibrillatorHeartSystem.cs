using Content.Medical.Shared.Body;
using Content.Shared.Body;
using Content.Shared.Medical;
using Content.Shared.Mobs.Components;

namespace Content.Medical.Server.Inkymed.Systems;

public sealed partial class DefibrillatorHeartSystem : EntitySystem // slop
{
    [Dependency] private HeartRateSystem _heartRate = default!;
    [Dependency] private BodySystem _body = default!;

    public static readonly ProtoId<OrganCategoryPrototype> HeartCategory = "Heart";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodyComponent, TargetDefibrillatedEvent>(OnFibbed);
    }

    private void OnFibbed(EntityUid target, BodyComponent body, ref TargetDefibrillatedEvent args)
    {
        var defib = args.Defibrillator.Comp;

        if (_body.GetOrgan(target, HeartCategory) is not {} heartUid
            || !TryComp<HeartComponent>(heartUid, out var heart))
            return;

        if (_heartRate.GetState(heart) == HeartState.Stopped)
        {
            _heartRate.SetRate(heartUid, heart, defib.BpmZapHealFlatline);
            return;
        }

        var sign = (heart.CurrentHeartRate > heart.StartingHeartRate) ^ defib.AutoStabilisation ? 1 : -1;
        var delta = sign * defib.BpmZapHeal;

        _heartRate.UpdateRate(heartUid, heart, delta);
    }
}
