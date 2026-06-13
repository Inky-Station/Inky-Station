using Content.Inky.Shared.Werewolf.Components;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Store.Components;
using Robust.Shared.Random;

namespace Content.Inky.Shared.Werewolf.Systems;

public sealed partial class SharedWerewolfBasicAbilitiesSystem
{
    public void InitializeBlack()
    {
        SubscribeLocalEvent<WerewolfBasicAbilitiesComponent, EventWerewolfBlackBite>(TryBite);
        SubscribeLocalEvent<WerewolfBasicAbilitiesComponent, WerewolfBlackBiteDoAfterEvent>(DoBite);

        SubscribeLocalEvent<WerewolfBasicAbilitiesComponent, EventWerewolfBequeath>(OnBequeath);
        SubscribeLocalEvent<WerewolfBasicAbilitiesComponent, MobStateChangedEvent>(OnLeaderDied);
    }

    private void TryBite(EntityUid uid, WerewolfBasicAbilitiesComponent comp, EventWerewolfBlackBite args)
    {
        if (TryComp<MobStateComponent>(args.Target, out var mobState) && mobState.CurrentState == MobState.Dead)
        {
            _popup.PopupEntity(Loc.GetString("werewolf-bite-fail-state"), uid, uid, PopupType.Large);
            return;
        }
        if (TryComp<WerewolfBitComponent>(args.Target, out var bit))
        {
            _popup.PopupEntity(Loc.GetString("werewolf-bite-fail-bit"), uid, uid, PopupType.Large);
            return;
        }
        if (TryComp<WerewolfInfectionImmuneComponent>(args.Target, out var immune)) // todo werewolf use for chaplain and holy stuff
        {
            _popup.PopupEntity(Loc.GetString("werewolf-bite-fail-immune"), uid, uid, PopupType.Large);
            return;
        }

        _popup.PopupEntity(Loc.GetString("werewolf-bite-start", ("user", uid), ("target", args.Target)), uid, uid, PopupType.LargeCaution); // todo locale

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(1), new WerewolfBlackBiteDoAfterEvent(), uid, args.Target)
        {
            DistanceThreshold = 1.5f,
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        });

        args.Handled = true;
    }

    private void DoBite(EntityUid uid, WerewolfBasicAbilitiesComponent comp, WerewolfBlackBiteDoAfterEvent args)
    {
        if (args.Cancelled || args.Target == null
                           || HasComp<WerewolfBitComponent>(args.Target)
                           || !TryComp<BodyComponent>(args.Target, out var body))
            return;

        SpillBloodPercentage(args.Target.Value, 30); // todo werewolf unhardcode
        args.Handled = true;

        var targetComp = EnsureComp<WerewolfBitComponent>(args.Target.Value);

        if (!_mind.TryGetMind(uid, out var mindId, out _)
            || !TryComp<WerewolfMindComponent>(mindId, out var mindComp))
            return;

        mindComp.Currency += comp.AmountDevour;
        mindComp.BittenPeople.Add(args.Target.Value);
        targetComp.BittenBy = mindComp;

        targetComp.Infected = _gambling.Prob(0.5f); // todo werewolf unhardcode the 50% chance?

        _audio.PlayPvs(comp.RipSound, uid);
    }

    private void OnBequeath(EntityUid uid, WerewolfBasicAbilitiesComponent comp, EventWerewolfBequeath args)
    {
        if (!_mind.TryGetMind(uid, out var leadMin, out _)
            || !TryComp<WerewolfMindComponent>(leadMin, out var leadMindComp))
            return;

        if (!leadMindComp.PackMembers.Contains(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("werewolf-bequeath-fail-not-pack"), uid, uid, PopupType.Large);
            return;
        }

        if (!_mind.TryGetMind(args.Target, out var targetMindId, out _))
            return;

        var bequeathed = EnsureComp<WerewolfBequeathedComponent>(targetMindId);
        bequeathed.OriginalLeader = leadMindComp;

        _popup.PopupEntity(Loc.GetString("werewolf-bequeath-success"), uid, uid, PopupType.Medium);
        args.Handled = true;
    }

    private void OnLeaderDied(EntityUid uid, WerewolfBasicAbilitiesComponent comp, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (!_mind.TryGetMind(uid, out var leaderMindId, out _)
            || !TryComp<WerewolfMindComponent>(leaderMindId, out var leaderMindComp))
            return;

        var eqe = EntityQueryEnumerator<WerewolfBequeathedComponent>();
        while (eqe.MoveNext(out var mindEnt, out var quComp))
        {
            if (quComp.OriginalLeader != leaderMindComp)
                continue;

            if (!TryComp<MindComponent>(mindEnt, out var mindComponent)
                || mindComponent.OwnedEntity is not { } quEnt)
                continue;

            if (!TryComp<WerewolfBasicAbilitiesComponent>(quEnt, out var wComp))
                continue;

            wComp.CurrentMutation = "WerewolfTransformBlack";

            var store = EnsureComp<StoreComponent>(quEnt);
            store.Categories.Add(quComp.Store);

            RemComp<WerewolfBequeathedComponent>(mindEnt);

            _popup.PopupEntity(Loc.GetString("werewolf-bequeath-triggered"), quEnt, quEnt, PopupType.LargeCaution);
        }
    }

    #region infection
    public void UpdateBlack(float frameTime) // not frametime but who carews
    {
        var query = EntityQueryEnumerator<WerewolfBitComponent>();
        while (query.MoveNext(out var uid, out var bit))
        {
            if (!bit.Infected)
                continue;

            bit.Accumulator += frameTime;

            if (bit.Accumulator < bit.LycTimer)
                continue;

            RemComp<WerewolfBitComponent>(uid);

            if (bit.BittenBy != null)
                bit.BittenBy.PackMembers.Add(uid);

            var ev = new WerewolfInfectionFinishedEvent(uid);
            RaiseLocalEvent(ref ev);
        }
    }

    #endregion
}
