using Content.Inky.Common.Events.Werewolf;
using Content.Inky.Shared.Werewolf;
using Content.Inky.Shared.Werewolf.Components;
using Content.Medical.Shared.Wounds;
using Content.Server.AlertLevel;
using Content.Server.Chat.Systems;
using Content.Server.Mind;
using Content.Server.Pinpointer;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Server.Store.Systems;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Polymorph;
using Content.Shared.Store.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Inky.Server.Werewolf.Systems;

public sealed partial class WerewolfAbilitiesSystem : EntitySystem
{
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private StoreSystem _store = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private HungerSystem _hunger = default!;

    // holy fuck
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private SharedBloodstreamSystem _blood = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private IRobustRandom _gambling = default!;
    [Dependency] private WoundSystem _wound = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private AlertLevelSystem _stationAlerts = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private MobThresholdSystem _mobThresholds = default!;
    [Dependency] private ActionContainerSystem _actionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WerewolfAbilitiesComponent, TransfurmEvent>(TryTransfurm);
        SubscribeLocalEvent<WerewolfAbilitiesComponent, EventWerewolfChangeType>(OnChangeType);
        SubscribeLocalEvent<WerewolfAbilitiesComponent, EventWerewolfOpenStore>(OnOpenStore);
        SubscribeLocalEvent<WerewolfAbilitiesComponent, PolymorphedEvent>(OnPolymorphed);
        SubscribeLocalEvent<WerewolfAbilitiesComponent, WerewolfActionRemoveEvent>(OnActionRemove);

        InitializeWerewolfSide();
        InitializeBlack();
    }

    # region basic handlers
    private void TryTransfurm(EntityUid uid,
        WerewolfAbilitiesComponent component,
        TransfurmEvent args)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out _)
            || !TryComp<WerewolfMindComponent>(mindId, out var mindComp))
            return;

        SyncMind(uid, component, mindComp);
        args.Handled = true; // if you add a return which shouldn't count as "handled" ADD IT BEFORE THIS

        if (mindComp.BlockTransfurm)
        {
            _popup.PopupEntity(Loc.GetString("werewolf-transfurm-block"), uid, uid);
            return;
        }

        if (!args.Forced && mindComp.Accumulator < mindComp.TransfurmOnCommandDelay)
        {
            var remainingTime = (mindComp.TransfurmOnCommandDelay - mindComp.Accumulator).TotalSeconds;
            _popup.PopupEntity(Loc.GetString("werewolf-transfurm-cooldown", ("remainingTime", remainingTime)), uid, uid);
            return;
        }

        mindComp.TransfurmReady = false;
        component.Transfurmed = false;
        mindComp.Accumulator = TimeSpan.Zero;

        if (component.Transfurmed)
        {
            _polymorph.Revert(uid);
            return;
        }

        component.Transfurmed = true;
        _polymorph.PolymorphEntity(uid, component.CurrentMutation);
        component.Transfurmed = false; // trust this is really important, the fucking polymorph is shit!!!!
    }

    private void OnPolymorphed(EntityUid uid, WerewolfAbilitiesComponent comp, PolymorphedEvent args)
    {
        if (!comp.Transfurmed)
        {
            _polymorph.CopyPolymorphComponent<HungerComponent>(uid, args.NewEntity);

            if (TryComp<HungerComponent>(uid, out var oldHunger)) // Transfer hunger value
                _hunger.SetHunger(args.NewEntity, _hunger.GetHunger(oldHunger));
            return;
        }

        if (TryComp<HungerComponent>(uid, out var oldHungerTakeTwo)) // Transfer hunger value
            _hunger.SetHunger(args.NewEntity, _hunger.GetHunger(oldHungerTakeTwo));

        var ev = new SelectFirstMartialArtEvent(args.NewEntity); // when you polymorph, it resets your current selected martial art
        RaiseLocalEvent(ev); // this is a very lazy solution but hey it works
    }

    private void OnOpenStore(Entity<WerewolfAbilitiesComponent> ent, ref EventWerewolfOpenStore args)
    {
        if (ent.Comp.Transfurmed)
            return;

        WerewolfMindComponent? mindComp = null;
        if (_mind.TryGetMind(ent, out var mindId, out _) && TryComp(mindId, out mindComp))
            SyncMind(ent, ent.Comp, mindComp);

        if (!TryComp<StoreComponent>(ent, out var store))
            return;

        // ok hear me out
        // when you do shit in the WW form that gives you points, it saves in mind and then the next time you open store it adds up
        // you HAVE to do ts because why? POLYMORPH IS FUCKING SHIT OF COURSE! ig you can store the old uid for store and shit but whatever
        if (mindComp != null && mindComp.Currency > 0)
        {
            _store.TryAddCurrency(new Dictionary<string, FixedPoint2> { { "Fury", mindComp.Currency } }, ent);
            mindComp.Currency = 0;
        }

        _store.ToggleUi(ent, ent, store);
        ent.Comp.StoreOpened = true;
    }

    private void OnChangeType(EntityUid uid, WerewolfAbilitiesComponent comp, EventWerewolfChangeType args)
    {
        comp.CurrentMutation = args.WerewolfType;
        Dirty(uid, comp);

        if (_mind.TryGetMind(uid, out var mindId, out _) && TryComp<WerewolfMindComponent>(mindId, out var mindComp))
            mindComp.CurrentMutation = args.WerewolfType;

        _popup.PopupEntity(Loc.GetString("werewolf-mutation-changed"), uid, uid);

        args.Handled = true;
    }

    private void OnActionRemove(EntityUid uid, WerewolfAbilitiesComponent comp, WerewolfActionRemoveEvent args)
    {
        _actionContainer.RemoveAction(args.ActionEnt);
    }

    private void SyncMind(EntityUid uid, WerewolfAbilitiesComponent comp, WerewolfMindComponent mindComp) // oh my god brother todo werewolf rename to be better
    {
        if (mindComp.CurrentMutation is { } currentMutation
            && comp.CurrentMutation != currentMutation)
        {
            comp.CurrentMutation = currentMutation;
            Dirty(uid, comp);
        }

        var store = EnsureComp<StoreComponent>(uid);
        foreach (var category in mindComp.StoreCategories)
            store.Categories.Add(category);
    }
    #endregion
}
