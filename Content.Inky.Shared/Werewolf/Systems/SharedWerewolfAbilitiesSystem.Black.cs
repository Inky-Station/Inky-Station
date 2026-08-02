using Content.Inky.Shared.Werewolf.Components;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Inky.Shared.Werewolf.Systems;

public sealed partial class SharedWerewolfAbilitiesSystem
{
    private readonly ProtoId<PolymorphPrototype> _werewolfTransformBlack = "WerewolfTransformBlack";

    public void InitializeBlack()
    {
        SubscribeLocalEvent<WerewolfAbilitiesComponent, EventWerewolfBlackBite>(TryBite);
        SubscribeLocalEvent<WerewolfAbilitiesComponent, WerewolfBlackBiteDoAfterEvent>(DoBite);

        SubscribeLocalEvent<WerewolfAbilitiesComponent, EventWerewolfBequeath>(OnBequeath);
        SubscribeLocalEvent<WerewolfAbilitiesComponent, MobStateChangedEvent>(OnLeaderDied);
    }

    private void TryBite(EntityUid uid, WerewolfAbilitiesComponent comp, EventWerewolfBlackBite args)
    {
        if (TryComp<MobStateComponent>(args.Target, out var mobState) && mobState.CurrentState == MobState.Dead)
        {
            _popup.PopupEntity(Loc.GetString("werewolf-bite-fail-state"), uid, uid, PopupType.Large);
            return;
        }
        if (HasComp<WerewolfBitComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("werewolf-bite-fail-bit"), uid, uid, PopupType.Large);
            return;
        }
        if (HasComp<WerewolfInfectionImmuneComponent>(args.Target)) // todo werewolf use for chaplain and holy stuff
        {
            _popup.PopupEntity(Loc.GetString("werewolf-bite-fail-immune"), uid, uid, PopupType.Large);
            return;
        }
        if (HasComp<WerewolfAbilitiesComponent>(args.Target))
        {
            _popup.PopupPredicted(Loc.GetString("werewolf-devour-fail-werewolf"), uid, uid); // no vore
            return;
        }

        _popup.PopupEntity(Loc.GetString("werewolf-bite-start", ("user", uid), ("target", args.Target)), uid, uid, PopupType.LargeCaution);

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

    private void DoBite(EntityUid uid, WerewolfAbilitiesComponent comp, WerewolfBlackBiteDoAfterEvent args)
    {
        if (args.Cancelled
            || args.Target == null
            || HasComp<WerewolfBitComponent>(args.Target)
            || !HasComp<BodyComponent>(args.Target))
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

        targetComp.Infected = _gambling.Prob(0.65f); // todo werewolf unhardcode the 65% chance?

        _audio.PlayPvs(comp.RipSound, uid);
    }

    private void OnBequeath(EntityUid uid, WerewolfAbilitiesComponent comp, EventWerewolfBequeath args)
    {
        if (!_mind.TryGetMind(uid, out var leadMind, out _)
            || !TryComp<WerewolfMindComponent>(leadMind, out var leadMindComp)
            || !_mind.TryGetMind(args.Target, out var targetMindId, out _))
            return;

        if (!leadMindComp.PackMembers.Contains(targetMindId))
        {
            _popup.PopupEntity(Loc.GetString("werewolf-bequeath-fail-not-pack"), uid, uid, PopupType.Large);
            return;
        }

        EnsureComp<WerewolfBequeathedComponent>(targetMindId).OriginalLeader = leadMindComp;

        _popup.PopupEntity(Loc.GetString("werewolf-bequeath-success"), uid, uid, PopupType.Medium);
        args.Handled = true;

        RaiseLocalEvent(uid, new WerewolfActionRemoveEvent(args.Action)); // one time use FUCK THEM PROPER ECS INFRASTRUCTURE NO comp.OneTimeUse
    }

    private void OnLeaderDied(EntityUid uid, WerewolfAbilitiesComponent comp, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (!_mind.TryGetMind(uid, out var leaderMindId, out _)
            || !TryComp<WerewolfMindComponent>(leaderMindId, out var leaderMindComp))
            return;

        var eqe = EntityQueryEnumerator<WerewolfBequeathedComponent>();
        while (eqe.MoveNext(out var mindEnt, out var bequeathed))
        {
            if (bequeathed.OriginalLeader != leaderMindComp
                || !TryComp<MindComponent>(mindEnt, out var mindComponent)
                || mindComponent.OwnedEntity is not { } bequeathedEnt
                || !TryComp<WerewolfAbilitiesComponent>(bequeathedEnt, out var wolf))
                continue;

            wolf.CurrentMutation = _werewolfTransformBlack;
            Dirty(bequeathedEnt, wolf);

            if (TryComp<WerewolfMindComponent>(mindEnt, out var werewolfMind))
            {
                werewolfMind.CurrentMutation = _werewolfTransformBlack;
                werewolfMind.StoreCategories.Add(bequeathed.Store);
            }

            var store = EnsureComp<StoreComponent>(bequeathedEnt);
            store.Categories.Add(bequeathed.Store);

            RemComp<WerewolfBequeathedComponent>(mindEnt);

            _popup.PopupEntity(Loc.GetString("werewolf-bequeath-triggered"), bequeathedEnt, bequeathedEnt, PopupType.LargeCaution);
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

            bit.Accumulator += TimeSpan.FromSeconds(frameTime);

            if (bit.Accumulator < bit.LycTimer)
                continue;

            RemComp<WerewolfBitComponent>(uid);

            if (bit.BittenBy != null && _mind.TryGetMind(uid, out var mind, out _))
                bit.BittenBy.PackMembers.Add(mind);

            var ev = new WerewolfInfectionFinishedEvent(uid);
            RaiseLocalEvent(ref ev);
        }
    }
    #endregion
}
