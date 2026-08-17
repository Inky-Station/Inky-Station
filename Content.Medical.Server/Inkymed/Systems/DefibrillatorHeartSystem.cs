using Content.Medical.Shared.Body;
using Content.Medical.Shared.Inkymed;
using Content.Shared.Body;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Medical;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Medical.Server.Inkymed.Systems;

public sealed partial class DefibrillatorHeartSystem : EntitySystem // slop
{
    [Dependency] private HeartRateSystem _heartRate = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private ItemToggleSystem _itemToggle = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ManualDefibrillatorSystem _manualDefibrillator = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    public static readonly ProtoId<OrganCategoryPrototype> HeartCategory = "Heart";
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();
        _nextUpdate = _timing.CurTime + UpdateInterval;

        SubscribeLocalEvent<BodyComponent, TargetDefibrillatedEvent>(OnFibbed);
        SubscribeLocalEvent<ManualDefibrillatorComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ManualDefibrillatorComponent, BoundUIClosedEvent>(OnUiClosed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        var eqe = EntityQueryEnumerator<ManualDefibrillatorComponent>();
        while (eqe.MoveNext(out var uid, out var defibrillator))
        {
            UpdateMonitor((uid, defibrillator));
        }
    }

    private void OnAfterInteract(Entity<ManualDefibrillatorComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled
            || _itemToggle.IsActivated(ent.Owner)
            || !args.CanReach
            || args.Target is not { } target
            || _body.GetOrgan(target, HeartCategory) is not {} heartUid
            || !TryComp<HeartComponent>(heartUid, out var heart))
            return;

        ent.Comp.HeartEntity = (heartUid, heart);
        SetMonitorState(ent, heart.CurrentRate); // goida
        _ui.OpenUi(ent.Owner, ManualDefibrillatorUiKey.Key, args.User);
    }

    private void OnUiClosed(Entity<ManualDefibrillatorComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!args.UiKey.Equals(ManualDefibrillatorUiKey.Key))
            return;

        ResetMonitor(ent);
    }

    private void UpdateMonitor(Entity<ManualDefibrillatorComponent> ent)
    {
        if (ent.Comp.HeartEntity is not { } heart
            || Deleted(heart.Owner)
            || !_interaction.InRangeUnobstructed(ent.Owner, heart.Owner))
        {
            ResetMonitor(ent);
            return;
        }

        SetMonitorState(ent, heart.Comp.CurrentRate);
    }

    private void ResetMonitor(Entity<ManualDefibrillatorComponent> ent)
    {
        ent.Comp.HeartEntity = null;
        Dirty(ent);
        SetMonitorState(ent, 0f);
    }

    private void SetMonitorState(Entity<ManualDefibrillatorComponent> ent, float bpm)
    {
        var pulse = GetPulseState(bpm);
        ent.Comp.PulseState = pulse;
        _manualDefibrillator.UpdateUi(ent);
    }

    private static ProtoId<PulseStatePrototype> GetPulseState(float bpm)
    {
        return bpm switch // KILL MEeeeee TODO KILL ME FASTER PLEASEEEEEEEEEEEEEEEEEEEEEEE
        {
            <= 0f => "Pulse0",
            <= 20f => "Pulse20",
            <= 30f => "Pulse30",
            <= 40f => "Pulse40",
            <= 50f => "Pulse50",
            <= 60f => "Pulse60",
            <= 70f => "Pulse70",
            <= 80f => "Pulse80",
            <= 100f => "Pulse100",
            <= 120f => "Pulse120",
            <= 140f => "Pulse140",
            <= 160f => "Pulse160",
            <= 180f => "Pulse180",
            <= 200f => "Pulse200",
            <= 220f => "Pulse220",
            <= 240f => "Pulse240",
            <= 260f => "Pulse260",
            <= 280f => "Pulse280",
            <= 300f => "Pulse300",
            <= 360f => "Pulse360",
            <= 400f => "Pulse400",
            <= 450f => "Pulse450",
            <= 999f => "Pulse450",
            _ => "Pulse3000", // todo inkymed
        };
    }

    private void OnFibbed(EntityUid target, BodyComponent body, ref TargetDefibrillatedEvent args)
    {
        var defib = args.Defibrillator.Comp;

        if (_body.GetOrgan(target, HeartCategory) is not {} heartUid
            || !TryComp<HeartComponent>(heartUid, out var heart))
            return;

        if (TryComp<ManualDefibrillatorComponent>(args.Defibrillator, out var manualDefibrillator))
        {
            manualDefibrillator.PulseState = "Pulse3000";
            _manualDefibrillator.UpdateUi((args.Defibrillator, manualDefibrillator));
        }

        if (_heartRate.GetState(heart) == HeartState.Stopped)
        {
            _heartRate.SetRate(heartUid, heart, defib.BpmZapHealFlatline, true);
            return;
        }

        // if we're above normal and we have autostabilisation multiply by -1
        var sign = (heart.CurrentRate > heart.NormalRate) && defib.AutoStabilisation ? -1 : 1;
        var delta = sign * defib.BpmZapHeal;
        _heartRate.UpdateRate(heartUid, heart, delta, false);
    }
}
