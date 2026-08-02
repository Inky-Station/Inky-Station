using System.Numerics;
using Content.Inky.Shared.Werewolf.Components;
using Content.Shared.Actions;
using Content.Shared.Camera;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Station;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Inky.Shared.Werewolf.Systems;

public sealed partial class SharedWerewolfAbilitiesSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private ActionContainerSystem _actionCon = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private SharedCameraRecoilSystem _recoil = default!;
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
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private IRobustRandom _gambling = default!;

    private float _updateTimer = 0f;
    /*
     * transfurmevent triggers polymorph shitcode that alters WerewolfAbilitiesComponent
     * which makes the eqe shit itself and crash the server
     * so we are collecting ents that need to transform to proccess them after
     */
    private readonly List<EntityUid> _transfurmQueue = [];

    public override void Initialize()
    {
        SubscribeLocalEvent<WerewolfAbilitiesComponent, HowlEvent>(DoHowl);
        SubscribeLocalEvent<WerewolfAbilitiesComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<WerewolfAbilitiesComponent, EventWerewolfUpgradeAbility>(OnUpgradeAbility);

        SubscribeLocalEvent<WerewolfAbilitiesComponent, WerewolfAmbushActionEvent>(OnAmbush);
        SubscribeLocalEvent<WerewolfAbilitiesComponent, ThrowDoHitEvent>(OnHit);

        SubscribeLocalEvent<WerewolfAbilitiesComponent, EventWerewolfRegen>(TryRegen);

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

        _transfurmQueue.Clear();

        var eqe = EntityQueryEnumerator<WerewolfAbilitiesComponent>();
        while (eqe.MoveNext(out var ent))
        {
            var uid = ent.Owner;
            if (!_mind.TryGetMind(uid, out var mindId, out _)
                || !TryComp<WerewolfMindComponent>(mindId, out var mindComp)
                || mindComp.BlockTransfurm)
                continue;

            mindComp.Accumulator += TimeSpan.FromSeconds(_updateTimer);

            if (mindComp.Accumulator >= mindComp.TransfurmWarnDelay && !mindComp.HasWarned)
            {
                _popup.PopupEntity(Loc.GetString(mindComp.TransfurmPopup), uid, uid, PopupType.LargeCaution);
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
                _transfurmQueue.Add(uid);
            }
        }

        foreach (var uid in _transfurmQueue)
            RaiseLocalEvent(uid, new TransfurmEvent());

        UpdateMark(timePassed);
        UpdateBlack(timePassed); // if there would ever be an infection cure for this, use same shit as _transfurmQueue because it'll probably make eqe shit itself too
    }

    private readonly ProtoId<TagPrototype> _furryTag = "VulpEmotes"; // kill yourself
    public void OnStartup(EntityUid uid, WerewolfAbilitiesComponent comp, ref ComponentStartup args)
    {
        if (_mind.TryGetMind(uid, out var mindId, out _)
            && TryComp<WerewolfMindComponent>(mindId, out var mindComp)
            && mindComp.CurrentMutation is { } currentMutation)
        {
            comp.CurrentMutation = currentMutation;
            return;
        }

        if (_tag.HasTag(uid, _furryTag))
        {
            comp.CurrentMutation = "WerewolfTransformWerehuman"; // TODO WEREWOLF unshit CurrentMutation to not use fucking string??? are you fucking retarded?????
            return;
        }
        comp.CurrentMutation = "WerewolfTransformBasic"; // goida
    }

    # region action handlers
    private void DoHowl(EntityUid uid, WerewolfAbilitiesComponent comp, ref HowlEvent args) //kill me for copying changeling system please
    {
        _audio.PlayPredicted(comp.ShriekSound, uid, uid);

        var victims = _entityLookup.GetEntitiesInRange(uid, args.ShriekPower);
        foreach (var victim in victims)
        {
            var distance = Transform(victim).WorldPosition - Transform(uid).WorldPosition;
            if (distance.EqualsApprox(Vector2.Zero))
                distance = new(1, 0);
            _recoil.KickCamera(uid, -distance.Normalized());

            _stun.TryUpdateStunDuration(victim, TimeSpan.FromSeconds(args.StunDuration));
            _stun.TryKnockdown(victim, TimeSpan.FromSeconds(args.StunDuration), true);
        }

        if (args.ForceTransfurm || args.HealNearby)
        {
            var pack = args.PackOnly && _mind.TryGetMind(uid, out var mindId, out _) && TryComp<WerewolfMindComponent>(mindId, out var mindComp)
                ? mindComp.PackMembers
                : null;

            foreach (var wolf in _entityLookup.GetEntitiesInRange(uid, args.ShriekPower))
            {
                if (!HasComp<WerewolfAbilitiesComponent>(wolf))
                    continue;

                if (pack != null
                    && (!_mind.TryGetMind(wolf, out var mind, out _)
                        || !pack.Contains(mind)))
                    continue;

                if (args.ForceTransfurm)
                    RaiseLocalEvent(wolf, new TransfurmEvent(true));

                if (args.HealNearby)
                    RaiseLocalEvent(wolf, new EventWerewolfRegen());
            }
        }
        _audio.PlayGlobal(comp.DistantSound, uid, AudioParams.Default.WithVolume(-30f)); // when you howl, everyone on the station hears a quiet distant howl, which breaks the metashield for the chaplain, "allegedly" todo uncomment when better sound is found
        args.Handled = true;
    }

    private void OnAmbush(EntityUid uid, WerewolfAbilitiesComponent comp, WerewolfAmbushActionEvent args) // partially taken from xenos jump
    {
        if (args.Handled
            || _container.IsEntityInContainer(uid))
            return;

        _throwing.TryThrow(uid, args.Target, args.JumpSpeed, uid, 10F);
        // todo PlayPVS
        args.Handled = true;
    }

    private void OnHit(EntityUid uid, WerewolfAbilitiesComponent comp, ThrowDoHitEvent args)
    {
        // if (args.Handled)
        //     return;

        _throwingItem.StopThrow(uid, args.Component);
        _stun.TryKnockdown(args.Target, TimeSpan.FromSeconds(1), true);

        // args.Handled = true;
    }
    #endregion

    #region store related shit
    /// <summary>
    /// Deletes and replaces the args.OldActionId with the args.NewActionId, also adding it to the mind
    /// </summary>
    private void OnUpgradeAbility(EntityUid uid, WerewolfAbilitiesComponent comp, EventWerewolfUpgradeAbility args)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out _)
            || !TryComp<WerewolfMindComponent>(mindId, out var mindComp))
            return;

        // update the mind to have those new actions
        if (args.OldActionId != null) // holy fucking kill myself
        {
            mindComp.UnlockedActions.Remove(args.OldActionId.Value);
            if (_actions.TryGetActionById(mindId, args.OldActionId.Value, out var oldAction))
                _actionCon.RemoveAction(oldAction.Value.AsNullable());
            else if (_actions.TryGetActionById(uid, args.OldActionId.Value, out var oldAttachedAction))
                _actions.RemoveAction(uid, oldAttachedAction.Value.AsNullable());
        }

        if (!mindComp.UnlockedActions.Contains(args.NewActionId))
            mindComp.UnlockedActions.Add(args.NewActionId);

        var action = _actionCon.AddAction(mindId, args.NewActionId);
        if (action != null)
            _actions.GrantContainedAction(uid, mindId, action.Value);

        _popup.PopupEntity(Loc.GetString("werewolf-ability-upgraded"), uid, uid);
        args.Handled = true;
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

    private void TryRegen(EntityUid uid, WerewolfAbilitiesComponent comp, EventWerewolfRegen args)
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
