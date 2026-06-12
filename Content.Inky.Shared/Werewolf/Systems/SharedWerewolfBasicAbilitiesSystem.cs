using System.Numerics;
using Content.Inky.Shared.Werewolf.Components;
using Content.Medical.Shared.Wounds;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Body.Systems;
using Content.Shared.Camera;
using Content.Shared.Chat;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Mind;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Pinpointer;
using Content.Shared.Popups;
using Content.Shared.Station;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Inky.Shared.Werewolf.Systems;

public sealed partial class SharedWerewolfBasicAbilitiesSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private EntityLookupSystem _entityLookup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private TagSystem _tag = default!;

    [Dependency] private ThrownItemSystem _throwingItem = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private SharedPuddleSystem _puddle = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedStationSystem _station = default!;
    [Dependency] private IMapManager _mapMan = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private IRobustRandom _gambling = default!;

    private float _updateTimer = 0f;

    public override void Initialize()
    {
        SubscribeLocalEvent<WerewolfBasicAbilitiesComponent, HowlEvent>(DoHowl);
        SubscribeLocalEvent<WerewolfBasicAbilitiesComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<WerewolfBasicAbilitiesComponent, EventWerewolfUpgradeAbility>(OnUpgradeAbility);

        SubscribeLocalEvent<WerewolfBasicAbilitiesComponent, WerewolfAmbushActionEvent>(OnAmbush);
        SubscribeLocalEvent<WerewolfBasicAbilitiesComponent, ThrowDoHitEvent>(OnHit);

        SubscribeLocalEvent<WerewolfBasicAbilitiesComponent, EventWerewolfRegen>(TryRegen);

        InitializeDire();
        InitializeWhite();
        InitializeBlack();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _updateTimer += frameTime;
        if (_updateTimer < 0.5f)
            return;

        var timePassed = _updateTimer;
        _updateTimer = 0f;

        var eqe = EntityQueryEnumerator<WerewolfBasicAbilitiesComponent>();
        while (eqe.MoveNext(out var uid, out var comp))
        {
            if (!_mind.TryGetMind(uid, out var mindId, out _)
                || !TryComp<WerewolfMindComponent>(mindId, out var mindComp)
                || mindComp.BlockTransfurm)
                continue;

            mindComp.Accumulator += timePassed;

            if (mindComp.Accumulator >= mindComp.TransfurmWarnDelay && !mindComp.HasWarned)
            {
                _popup.PopupEntity(Loc.GetString(mindComp.TransfurmPopup), uid, uid, PopupType.LargeCaution); // todo werewolf predict
                mindComp.HasWarned = true;
            }

            if (mindComp.Accumulator >= mindComp.TransfurmOnCommandDelay && !mindComp.TransfurmReady)
            {
                _popup.PopupEntity(Loc.GetString(mindComp.TransfurmReadyPopup), uid, uid, PopupType.Medium);
                mindComp.TransfurmReady = true;
            }

            if (mindComp.Accumulator >= mindComp.TransfurmCycle)
            {
                mindComp.TransfurmReady = false;
                mindComp.HasWarned = false;

                RaiseLocalEvent(uid, new TransfurmEvent());
            }
        }
        UpdateMark(timePassed);
        UpdateBlack(timePassed);
    }

    private const string DogTag = "VulpEmotes";
    public void OnStartup(EntityUid uid, WerewolfBasicAbilitiesComponent comp, ref ComponentStartup args)
    {
        if (_tag.HasTag(uid, DogTag))
        {
            comp.CurrentMutation = "WerewolfTransformWerehuman"; // TODO WEREWOLF unshit CurrentMutation to not use fucking string??? are you fucking retarded?????
            return;
        }
        comp.CurrentMutation = "WerewolfTransformBasic"; // goida
    }

    # region action handlers
    private void DoHowl(EntityUid uid, WerewolfBasicAbilitiesComponent comp, ref HowlEvent args) //kill me for copying changeling system please
    {
        _audio.PlayPredicted(comp.ShriekSound, uid, uid);

        var center = Transform(uid).MapPosition;
        var gamers = Filter.Empty();
        gamers.AddInRange(center, args.ShriekPower, _player, EntityManager);

        foreach (var gamer in gamers.Recipients)
        {
            if (gamer.AttachedEntity == null)
                continue;

            var pos = Transform(gamer.AttachedEntity!.Value).WorldPosition;
            var delta = center.Position - pos;

            if (delta.EqualsApprox(Vector2.Zero))
                delta = new(.01f, 0);

            _recoil.KickCamera(uid, -delta.Normalized());
            foreach (var entity in _entityLookup.GetEntitiesInRange(uid, args.ShriekPower))
            {
                _stun.TryUpdateStunDuration(entity, TimeSpan.FromSeconds(args.StunDuration));
                _stun.TryKnockdown(entity, TimeSpan.FromSeconds(args.StunDuration), true);
            }
        }

        if (args.ForceTransfurm || args.HealNearby)
        {
            List<EntityUid>? pack = null;
            if (args.PackOnly)
            {
                if (!_mind.TryGetMind(uid, out var mindId, out _)
                    || !TryComp<WerewolfMindComponent>(mindId, out var mindComp))
                    return;

                pack = mindComp.PackMembers;
            }

            foreach (var wolf in _entityLookup.GetEntitiesInRange(uid, args.ShriekPower))
            {
                if (!HasComp<WerewolfBasicAbilitiesComponent>(wolf))
                    continue;

                if (pack != null && !pack.Contains(wolf)) // oughhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh
                    continue;

                if (args.ForceTransfurm)
                    RaiseLocalEvent(wolf, new TransfurmEvent(true));

                if (args.HealNearby)
                    RaiseLocalEvent(wolf, new EventWerewolfRegen());
            }
        }
        // _audio.PlayGlobal(comp.DistantSound, uid); // when you howl, everyone on the station hears a quiet distant howl, which breaks the metashield for the chaplain, "allegedly" todo uncomment when better sound is found
        args.Handled = true;
    }
    private void OnAmbush(EntityUid uid, WerewolfBasicAbilitiesComponent comp, WerewolfAmbushActionEvent args) // partially taken from xenos jump
    {
        if (args.Handled
            || _container.IsEntityInContainer(uid))
            return;

        _throwing.TryThrow(uid, args.Target, args.JumpSpeed, uid, 10F);
        // todo PlayPVS
        args.Handled = true;
    }

    private void OnHit(EntityUid uid, WerewolfBasicAbilitiesComponent comp, ThrowDoHitEvent args)
    {
        // if (args.Handled)
        //     return;

        _throwingItem.StopThrow(uid, args.Component);

        if (Transform(args.Target).Anchored)
            _stun.TryUpdateParalyzeDuration(uid, TimeSpan.FromSeconds(1));
        else
            _stun.TryKnockdown(args.Target, TimeSpan.FromSeconds(1), true);

        // args.Handled = true;
    }
    #endregion

    #region store related shit
    /// <summary>
    /// Deletes and replaces the args.OldActionId with the args.NewActionId, also adding it to the mind
    /// </summary>
    private void OnUpgradeAbility(EntityUid uid, WerewolfBasicAbilitiesComponent comp, EventWerewolfUpgradeAbility args)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out _)
            || !TryComp<WerewolfMindComponent>(mindId, out var mindComp))
            return;

        // update the mind to have those new actions
        if (args.OldActionId != null)
            mindComp.UnlockedActions.Remove(args.OldActionId);
        if (!mindComp.UnlockedActions.Contains(args.NewActionId))
            mindComp.UnlockedActions.Add(args.NewActionId);

        SyncActions(uid, comp);

        _popup.PopupEntity(Loc.GetString("werewolf-ability-upgraded"), uid, uid);
        args.Handled = true;
    }

    // used for polymorph ent recieving actions from the mind
    public void SyncActions(EntityUid uid, WerewolfBasicAbilitiesComponent comp) // todo the SERVER gives out an error when you polymorph, tries to remove shit that isnt there, fix before merg Attempted to remove an action Howl (9413/n9413, ActionWerewolfHowl) from an entity that it was never attached to: wolf
    {
        // foreach (var actionEnt in comp.ActionEntities.Values)
        //     if (TryComp<ActionComponent>(actionEnt, out var actComp) && actComp.AttachedEntity == uid) // dont remove stuff from the wolf if it doesnt exist
        //         _actions.RemoveAction(uid, actionEnt);
        foreach (var actionEnt in comp.ActionEntities.Values)
            _actions.RemoveAction(uid, actionEnt);
        comp.ActionEntities.Clear();

        // if the mind has unlocked actions, use those
        if (_mind.TryGetMind(uid, out var mindId, out _) && TryComp<WerewolfMindComponent>(mindId, out var mindComp))
        {
            foreach (var actionId in mindComp.UnlockedActions)
            {
                var actionEnt = _actions.AddAction(uid, actionId);
                if (actionEnt != null)
                    comp.ActionEntities[actionId] = actionEnt.Value;
            }
        }
        else
        {// if for some reason not, use starting actions
            foreach (var actionId in comp.WerewolfActions)
            {
                var actionEnt = _actions.AddAction(uid, actionId);
                if (actionEnt != null)
                    comp.ActionEntities[actionId] = actionEnt.Value;
            }
        }
    }
    #endregion


    public bool TryInjectReagents(EntityUid uid, Dictionary<string, FixedPoint2> reagents)
    {
        var solution = new Solution();
        foreach (var (reagentId, quantity) in reagents)
            solution.AddReagent(reagentId, quantity);

        if (!_solution.TryGetInjectableSolution(uid, out var targetSolution, out _))
            return false;

        return _solution.TryAddSolution(targetSolution.Value, solution);
    }

    private void TryRegen(EntityUid uid, WerewolfBasicAbilitiesComponent comp, EventWerewolfRegen args)
    {
        var reagents = new Dictionary<string, FixedPoint2> // i hate fixedpoint bru // todo werewolf unhardcode, put into a comp idk
        {
            ["Ichor"] = FixedPoint2.New(10),
            ["TranexamicAcid"] = FixedPoint2.New(5)
        };

        if (TryInjectReagents(uid, reagents))
            _popup.PopupPredicted(Loc.GetString("werewolf-action-regen-success"), uid, uid);
        args.Handled = true;
    }
}
