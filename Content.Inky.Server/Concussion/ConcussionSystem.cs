using Content.Goobstation.Shared.Flashbang;
using Content.Inky.Common.CCVar;
using Content.Inky.Common.Concussion;
using Content.Inky.Shared.Concussion;
using Content.Server.Ghost;
using Content.Server.Speech.Components;
using Content.Shared.Alert;
using Content.Shared.Damage.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Explosion;
using Content.Shared.FixedPoint;
using Content.Shared.Gibbing;
using Content.Shared.Inventory;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Movement.Systems;
using Content.Shared.Rejuvenate;
using Content.Shared.Speech.Components;
using Content.Trauma.Common.Language.Components;
using Content.Trauma.Common.Language.Systems;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Inky.Server.Concussion;

public sealed class ConcussionSystem : SharedConcussionSystem
{
    [Dependency] private readonly CommonLanguageSystem _theTowerOfBabel = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;

    private ProtoId<AlertPrototype> _alert = "Concussion";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConcussionThresholdComponent, BeforeExplodeEvent>(OnBeforeExplode);
        SubscribeLocalEvent<ConcussionThresholdComponent, ConcussionStateChangedEvent>(OnConcussion);
        SubscribeLocalEvent<ConcussionThresholdComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<ConcussionThresholdComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<ConcussionThresholdComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<ConcussionThresholdComponent, GhostAttemptHandleEvent>(OnGhostAttempt);
        SubscribeLocalEvent<ConcussionThresholdComponent, GetFlashbangedEvent>(OnFlashbanged);
        SubscribeLocalEvent<ConcussionThresholdComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);

        SubscribeLocalEvent<ConcussedComponent, ComponentInit>(OnConcussed);
        SubscribeLocalEvent<ConcussedComponent, ComponentShutdown>(OnDeConcussed);
    }
    #region logic
    private void OnMapInit(EntityUid uid, ConcussionThresholdComponent comp, MapInitEvent args)
        => comp.NextUpdate = _timing.CurTime + comp.UpdateInterval;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        var eqe = EntityQueryEnumerator<ConcussionThresholdComponent>();
        while (eqe.MoveNext(out var uid, out var comp))
        {
            if (comp.StoredDamage == FixedPoint2.Zero)
                continue;

            if (curTime < comp.NextUpdate)
                continue;

            var elapsed = (curTime - (comp.NextUpdate - comp.UpdateInterval)).TotalSeconds;
            var healing = comp.HealRate * elapsed;
            comp.StoredDamage = FixedPoint2.Max(FixedPoint2.Zero, comp.StoredDamage - healing);

            UpdateConcussionState(uid, comp);
            Dirty(uid, comp);
            comp.NextUpdate = curTime + comp.UpdateInterval;
        }
    }

    private void OnConcussion(EntityUid uid, ConcussionThresholdComponent comp, ConcussionStateChangedEvent args)
    {
        _movement.RefreshMovementSpeedModifiers(uid);

        if (args.NewState != ConcussionState.Sane)
        {
            EnsureComp<ConcussedComponent>(uid);
            EnsureComp<SlurredAccentComponent>(uid);
        }
        else
        {
            RemComp<ConcussedComponent>(uid);
            RemComp<SlurredAccentComponent>(uid);
        }

        if (TryComp<LanguageSpeakerComponent>(uid, out var speaker))
            _theTowerOfBabel.UpdateEntityLanguages((uid, speaker));
    }

    private void OnBeforeExplode(EntityUid uid, ConcussionThresholdComponent comp, ref BeforeExplodeEvent args)
    {
        if (HasComp<GodmodeComponent>(uid)) // no concussion shite for admins or whatever
            return;

        var totalDmg = args.Damage.GetTotal();
        var concussionDmg = totalDmg * 10; // todo nuke this fucking hardcoded value to have concussion resistance in the future i cbb to do it rn
        AddConcussionDamage(uid, comp, concussionDmg); // also the x10 damage multiplier makes any explosion damage absolutely rape anyone
    }
    #endregion
    private void OnRejuvenate(EntityUid uid, ConcussionThresholdComponent comp, RejuvenateEvent ev)
        => comp.StoredDamage = FixedPoint2.New(1); // to update the overlay

    private void OnMindRemoved(EntityUid uid, ConcussionThresholdComponent comp, MindRemovedMessage ev)
        => comp.StoredDamage = FixedPoint2.New(1);

    private void OnGhostAttempt(EntityUid uid, ConcussionThresholdComponent comp, GhostAttemptHandleEvent ev)
        => comp.StoredDamage = FixedPoint2.New(1);

    private void OnFlashbanged(Entity<ConcussionThresholdComponent> ent, ref GetFlashbangedEvent args)
    {
        if (args.ConcussionDamage <= 0f)
            return;

        AddConcussionDamage(ent.Owner, ent.Comp, args.ConcussionDamage);
    }

    private void OnRefreshSpeed(EntityUid uid, ConcussionThresholdComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        if (!TryComp<ConcussionThresholdComponent>(uid, out var threshold))
            return;

        if (!comp.SpeedModifierThresholds.TryGetValue(threshold.CurrentState, out var speed))
            return;

        args.ModifySpeed(speed, speed);
    }

    private void OnConcussed(EntityUid uid, ConcussedComponent comp, ComponentInit args)
        => _alertsSystem.ShowAlert(uid, _alert);

    private void OnDeConcussed(EntityUid uid, ConcussedComponent comp, ComponentShutdown args)
        => _alertsSystem.ClearAlert(uid, _alert);
}
