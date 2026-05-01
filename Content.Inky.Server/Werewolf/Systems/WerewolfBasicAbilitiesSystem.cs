using Content.Inky.Shared.Werewolf;
using Content.Inky.Shared.Werewolf.Components;
using Content.Inky.Shared.Werewolf.Systems;
using Content.Medical.Shared.Wounds;
using Content.Server.Mind;
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
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Inky.Server.Werewolf.Systems;

public sealed partial class WerewolfBasicAbilitiesSystem : EntitySystem
{
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedWerewolfBasicAbilitiesSystem _werewolf = default!; // hell.
    [Dependency] private readonly HungerSystem _hunger = default!;

    // holy fuck
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedBloodstreamSystem _blood = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly IRobustRandom _gambling = default!;
    [Dependency] private readonly WoundSystem _wound = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WerewolfBasicAbilitiesComponent, TransfurmEvent>(TryTransfurm);
        SubscribeLocalEvent<WerewolfBasicAbilitiesComponent, EventWerewolfChangeType>(OnChangeType);
        SubscribeLocalEvent<WerewolfBasicAbilitiesComponent, EventWerewolfOpenStore>(OnOpenStore);
        SubscribeLocalEvent<WerewolfBasicAbilitiesComponent, PolymorphedEvent>(OnPolymorphed);

        InitializeWerewolfSide();
    }

    # region basic handlers
    private void TryTransfurm(EntityUid uid, WerewolfBasicAbilitiesComponent component, TransfurmEvent args)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out _)
            || !TryComp<WerewolfMindComponent>(mindId, out var mindComp))
            return;

        if (mindComp.Accumulator < mindComp.TransfurmOnCommandDelay)
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
            // RemComp<MartialArtsKnowledgeComponent>(uid); // bruh we love MA shitcod
            // RemComp<CanPerformComboComponent>(uid);
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
