using Content.Inky.Common.Medical;
using Content.Medical.Shared.Body;
using Content.Shared.Alert;
using Content.Shared.Body;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Rejuvenate;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Medical.Shared.Inkymed;

public sealed partial class HeartRateSystem : EntitySystem // todo godmode bypass
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private IRobustRandom _gambling = default!;

    private static readonly float HeartStop = 0f;
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();
        _nextUpdate = _timing.CurTime + UpdateInterval;

        SubscribeLocalEvent<HeartComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<HeartComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<BodyComponent, FindWorkingHeartEvent>(OnFindHeart);
    }

    private void OnComponentInit(EntityUid uid, HeartComponent heart, ComponentInit args)
    {
        SetRate(uid, heart, heart.NormalRate, true);
    }

    private void OnRejuvenate(EntityUid uid, HeartComponent heart, RejuvenateEvent args)
    {
        SetRate(uid, heart, heart.NormalRate, true);
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
        if ((organ.Body is not { } body // the heart is outside a body
            || !TryComp<MobStateComponent>(body, out var mobState) // or the body is not a mob
            || mobState.CurrentState == MobState.Dead // or the body is dead
            || heart.CurrentRate > heart.CriticalRate) // or the heart is beyond critical
        && _gambling.Prob(heart.CriticalStopChance)) // and also you're unlucky enough
            SetRate(uid, heart, HeartStop, false);

        // fibrillating drifts AWAY from the normal heart rate (towards min/max)
        // being stable drifts TOWARDS the normal heart rate
        var cur = heart.CurrentRate;
        var (min, max) = heart.FibrillationCaps;
        var delta = heart.RateUpdateModifier * (float) Math.Cbrt(
            (cur - heart.NormalRate) * (cur - min) * (cur - max)
        );

        UpdateRate(uid, heart, delta, false);
    }

    // goida idk
    private void OnFindHeart(Entity<BodyComponent> ent, ref FindWorkingHeartEvent args)
    {
        var hearts = _body.GetOrgans<HeartComponent>(ent.AsType());
        foreach (var heart in hearts)
            if (GetState(heart.Comp) != HeartState.Stopped)
            {
                args.Found = true;
                return;
            }
    }

    #region api

    public void SetRate(EntityUid uid,
        HeartComponent heart,
        float rate,
        bool canRestart)
    {
        var oldState = GetState(heart);
        if (oldState == HeartState.Stopped && !canRestart)
            return;

        // being at min or beyond just stops the heart
        heart.CurrentRate = Math.Max(rate, HeartStop);

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
        bool canRestart,
        float? lowCap = null,
        float? highCap = null)
    {
        var newRate = heart.CurrentRate + delta;

        if (lowCap is { } someLowCap && newRate < someLowCap)
            newRate = someLowCap;

        if (highCap is { } someHighCap && newRate > someHighCap)
            newRate = someHighCap;

        SetRate(uid, heart, newRate, canRestart);
    }

    // fuck invariants lmao
    public HeartState GetState(HeartComponent heart)
    {
        if (heart.CurrentRate <= HeartStop)
            return HeartState.Stopped;

        var (min, max) = heart.FibrillationCaps;
        if (heart.CurrentRate > max || heart.CurrentRate < min)
            return HeartState.Fibrillating;

        return HeartState.Stable;
    }

    #endregion
}
