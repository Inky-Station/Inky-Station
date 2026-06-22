using Content.Medical.Shared.Body;
using Content.Shared.Alert;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Rejuvenate;
using Robust.Shared.Timing;

namespace Content.Medical.Shared.Inkymed;

public sealed partial class HeartRateSystem : EntitySystem // todo godmode bypass
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextUpdate;

    private static readonly DamageSpecifier FlatlineDamage = new()
    {
        DamageDict = new Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>
        {
            { "Bloodloss", 8 } // goida hardcode gg
        }
    };

    public override void Initialize()
    {
        base.Initialize();
        _nextUpdate = _timing.CurTime + UpdateInterval;

        SubscribeLocalEvent<HeartComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<HeartComponent, RejuvenateEvent>(OnRejuvenate);
    }

    private void OnComponentInit(EntityUid uid, HeartComponent heart, ComponentInit args)
    {
        SetRate(uid, heart, heart.NormalHeartRate, true);
    }

    private void OnRejuvenate(EntityUid uid, HeartComponent heart, RejuvenateEvent args)
    {
        SetRate(uid, heart, heart.NormalHeartRate, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        var eqe = EntityQueryEnumerator<HeartComponent, OrganComponent>();
        while (eqe.MoveNext(out var uid, out var heart, out var organ))
        {
            UpdateHeart(uid, heart, organ);
        }
    }

    private void UpdateHeart(EntityUid uid, HeartComponent heart, OrganComponent organ)
    {
        if (organ.Body is not { } body // the heart is outside a body
            || !TryComp<MobStateComponent>(body, out var mobState) // or the body is not a mob
            || mobState.CurrentState == MobState.Dead) // or the body is dead
        {
            UpdateRate(uid, heart, -heart.StabilisationRate, false);
            return;
        }

        // fibrillating drifts AWAY from the normal heart rate (towards min/max)
        // being stable drifts TOWARDS the normal heart rate
        var sign = (heart.CurrentHeartRate < heart.NormalHeartRate) ^ (GetState(heart) == HeartState.Fibrillating) ? 1 : -1;
        var delta = sign * heart.StabilisationRate;
        UpdateRate(uid, heart, delta, false);

        // apparently if your heart is dead you take damage
        if (GetState(heart) == HeartState.Stopped)
            _damageable.TryChangeDamage(body, FlatlineDamage, ignoreResistances: true);
    }

    #region api

    public void SetRate(EntityUid uid,
        HeartComponent heart,
        float rate,
        bool canRestart)
    {
        var oldState = GetState(heart);
        if (GetState(heart) == HeartState.Stopped && !canRestart)
            return;

        // being at min/max or beyond just stops the heart
        if (rate <= heart.MinHeartRate
            || rate >= heart.MaxHeartRate)
            heart.CurrentHeartRate = heart.MinHeartRate;
        else
            heart.CurrentHeartRate = rate;

        var newState = GetState(heart);
        Dirty(uid, heart);

        if (oldState == newState // nothing changed
            || !TryComp<OrganComponent>(uid, out var organ) // or the heart is not an organ
            || organ.Body is not { } body) // or it is outside of body
            return;

        var ev = new HeartStateChangedEvent(oldState, newState);
        RaiseLocalEvent(body, ref ev);

        // update alerts
        if (heart.FibrillationAlert is { } fibAlert)
        {
            if (newState == HeartState.Fibrillating)
                _alerts.ShowAlert(body, fibAlert);
            else
                _alerts.ClearAlert(body, fibAlert);
        }

        if (heart.HeartStopAlert is { } stopAlert)
        {
            if (newState == HeartState.Stopped)
                _alerts.ShowAlert(body, stopAlert);
            else
                _alerts.ClearAlert(body, stopAlert);
        }
    }

    public void UpdateRate(EntityUid uid,
        HeartComponent heart,
        float delta,
        bool canRestart)
    {
        SetRate(uid, heart, heart.CurrentHeartRate + delta, canRestart);
    }

    // fuck invariants lmao
    public HeartState GetState(HeartComponent heart)
    {
        if (heart.CurrentHeartRate <= heart.MinHeartRate)
            return HeartState.Stopped;

        if (Math.Abs(heart.NormalHeartRate - heart.CurrentHeartRate) >= heart.FibrillationCap)
            return HeartState.Fibrillating;

        return HeartState.Stable;
    }

    #endregion
}
