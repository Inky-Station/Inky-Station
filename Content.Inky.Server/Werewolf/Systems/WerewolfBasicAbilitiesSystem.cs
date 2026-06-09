using Content.Inky.Common.Events.Werewolf;
using Content.Inky.Shared.Werewolf;
using Content.Inky.Shared.Werewolf.Components;
using Content.Inky.Shared.Werewolf.Systems;
using Content.Medical.Shared.Wounds;
using Content.Server.Chat.Systems;
using Content.Server.Mind;
using Content.Server.Pinpointer;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Server.Store.Systems;
using Content.Shared.Body;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Polymorph;
using Content.Shared.Store.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Inky.Server.Werewolf.Systems;

public sealed partial class WerewolfBasicAbilitiesSystem : EntitySystem
{
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private StoreSystem _store = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private SharedWerewolfBasicAbilitiesSystem _werewolf = default!; // hell.
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WerewolfBasicAbilitiesComponent, TransfurmEvent>(TryTransfurm);
        SubscribeLocalEvent<WerewolfBasicAbilitiesComponent, EventWerewolfChangeType>(OnChangeType);
        SubscribeLocalEvent<WerewolfBasicAbilitiesComponent, EventWerewolfOpenStore>(OnOpenStore);
        SubscribeLocalEvent<WerewolfBasicAbilitiesComponent, PolymorphedEvent>(OnPolymorphed);

        InitializeWerewolfSide();
        InitializeBlack();
    }

    # region basic handlers
    private void TryTransfurm(EntityUid uid,
        WerewolfBasicAbilitiesComponent component,
        TransfurmEvent args)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out _)
            || !TryComp<WerewolfMindComponent>(mindId, out var mindComp))
            return;

        if (mindComp.BlockTransfurm)
        {
            _popup.PopupEntity(Loc.GetString("werewolf-transfurm-block"), uid, uid);
            args.Handled = true;
            return;
        }

        if (!args.Forced && mindComp.Accumulator < mindComp.TransfurmOnCommandDelay)
        {
            _popup.PopupEntity(Loc.GetString("werewolf-transfurm-cooldown"), uid, uid); // todo werewolf locale & timeLeft
            args.Handled = true;
            return;
        }

        if (component.Transfurmed)
        {
            component.Transfurmed = false;
            mindComp.TransfurmReady = false;
            _polymorph.Revert(uid);
            // _sharedWerewolf.SyncActions(uid, component);
            args.Handled = true;
            mindComp.Accumulator = 0f;
            return;
        }

        component.Transfurmed = true;
        mindComp.TransfurmReady = false;
        _polymorph.PolymorphEntity(uid, component.CurrentMutation);
        component.Transfurmed = false; // trust this is really important, the fucking polymorph is shit!!!!
        mindComp.Accumulator = 0f;
        args.Handled = true;
    }

    private void OnPolymorphed(EntityUid uid, WerewolfBasicAbilitiesComponent comp, PolymorphedEvent args)
    {
        if (!comp.Transfurmed)
        {
            _polymorph.CopyPolymorphComponent<HungerComponent>(uid, args.NewEntity);

            if (TryComp<HungerComponent>(uid, out var oldHunger)) // Transfer hunger value
                _hunger.SetHunger(args.NewEntity, _hunger.GetHunger(oldHunger));
            return;
        }
        _polymorph.CopyPolymorphComponent<WerewolfBasicAbilitiesComponent>(uid, args.NewEntity);
        _polymorph.CopyPolymorphComponent<HungerComponent>(uid, args.NewEntity);

        if (TryComp<HungerComponent>(uid, out var oldHungerTakeTwo)) // Transfer hunger value
            _hunger.SetHunger(args.NewEntity, _hunger.GetHunger(oldHungerTakeTwo));

        // _sharedWerewolf.SyncActions(args.NewEntity, Comp<WerewolfBasicAbilitiesComponent>(args.NewEntity)); // todo
        var werewolf = Comp<WerewolfBasicAbilitiesComponent>(args.NewEntity);
        // werewolf.ActionEntities.Clear();
        _werewolf.SyncActions(args.NewEntity, werewolf);

        var ev = new SelectFirstMartialArtEvent(args.NewEntity); // when you polymorph, it resets your current selected martial art
        RaiseLocalEvent(ev); // this is a very lazy solution but hey it works
    }

    private void OnOpenStore(Entity<WerewolfBasicAbilitiesComponent> ent, ref EventWerewolfOpenStore args)
    {
        if (!TryComp<StoreComponent>(ent, out var store)
            || ent.Comp.Transfurmed == true)
            return;

        // ok hear me out
        // when you do shit in the WW form that gives you points, it saves in mind and then the next time you open store it adds up
        // you HAVE to do ts because why? POLYMORPH IS FUCKING SHIT OF COURSE! ig you can store the old uid for store and shit but whatever
        if (_mind.TryGetMind(ent, out var mindId, out _) && TryComp<WerewolfMindComponent>(mindId, out var mindComp))
        {
            if (mindComp.Currency > 0)
            {
                _store.TryAddCurrency(new Dictionary<string, FixedPoint2> {{ "Fury", mindComp.Currency }}, ent);
                mindComp.Currency = 0;
            }
        }

        _store.ToggleUi(ent, ent, store);
        ent.Comp.StoreOpened = true;
    }

    private void OnChangeType(EntityUid uid, WerewolfBasicAbilitiesComponent comp, EventWerewolfChangeType args)
    {
        comp.CurrentMutation = args.WerewolfType;
        _popup.PopupEntity(Loc.GetString("werewolf-mutation-changed", ("mutation", args.WerewolfType)), uid, uid); // todo locale

        args.Handled = true;
    }

    #endregion
}
