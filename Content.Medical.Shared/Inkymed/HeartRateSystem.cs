using Content.Inky.Common.Medical;
using Content.Medical.Shared.Body;
using Content.Shared.Alert;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
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
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    private static readonly float HeartStop = 0f;
    private static readonly FixedPoint2 LethalBloodVolume = FixedPoint2.New(360);
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
        if ((organ.Body is not { } body
            || !TryComp<MobStateComponent>(body, out var mobState)
            || mobState.CurrentState == MobState.Dead
            || heart.CurrentRate > heart.CriticalRate)
        && _gambling.Prob(heart.CriticalStopChance))
            SetRate(uid, heart, HeartStop, false);

        if (organ.Body is { } bodyEnt)
            ApplyBloodVolumeEffects(uid, heart, bodyEnt);

        var cur = heart.CurrentRate;
        var (min, max) = heart.FibrillationCaps;
        var delta = heart.RateUpdateModifier * (float) Math.Cbrt(
            (cur - heart.NormalRate) * (cur - min) * (cur - max)
        );

        UpdateRate(uid, heart, delta, false);
    }

    private void ApplyBloodVolumeEffects(EntityUid uid, HeartComponent heart, EntityUid body)
    {
        if (!TryComp<BloodstreamComponent>(body, out var bloodstream))
            return;

        var current = GetBloodVolume(body);

        if (current <= LethalBloodVolume)
        {
            SetRate(uid, heart, HeartStop, false);
            return;
        }

        var max = bloodstream.BloodReferenceSolution.Volume;
        if (max <= FixedPoint2.Zero)
            return;

        var missingRatio = 1f - current.Float() / max.Float();
        if (missingRatio <= 0f)
            return;

        heart.CurrentRate += heart.NormalRate * missingRatio * heart.RateUpdateModifier;
        Dirty(uid, heart);
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

        heart.CurrentRate = Math.Max(rate, HeartStop);

        var newState = GetState(heart);
        Dirty(uid, heart);

        if (oldState == newState
            || !TryComp<OrganComponent>(uid, out var organ)
            || organ.Body is not { } body)
            return;

        var ev = new HeartStateChangedEvent(oldState, newState);
        RaiseLocalEvent(body, ref ev);

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

    public FixedPoint2 GetBloodVolume(EntityUid uid)
    {
        if (!TryComp<BloodstreamComponent>(uid, out var bloodstream))
            return FixedPoint2.Zero;

        if (bloodstream.BloodSolution is not { } bloodSolutionEntity)
            return FixedPoint2.Zero;

        if (!_solutionContainer.ResolveSolution((uid, null), bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var solution))
            return FixedPoint2.Zero;

        return solution.Volume;
    }

    #endregion
}
