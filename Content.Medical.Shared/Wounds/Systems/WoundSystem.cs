// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.CCVar;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Traumas;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.CPUJob.JobQueues.Queues;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Medical.Shared.Wounds;

/*
 * inkymed documentation volume I:
 * Wounds, Woundables and YOU.
 * i am making this one because there isnt a proper documentation on this and the entirety of
 * wounds and woundmed as a whole, so this comment about to be few hundered lines long and i do not care about it.
 *
 * this system is the bridge between normal damageable damage and the woundmed damage
 * damage is still applied through the regular damage APIs, but when a body part has
 * WoundableComponent the damage is converted into WoundComponent ents held
 * inside the woundables wound container
 *
 * basically, to check it in game you:
 * - VV yourself
 * - select BodyComponent
 * - go into the Organs tab (the first container)
 * - go into _containerList
 * - click on the first ~6 listings, those are most likely limbs
 * - on those limbs find WoundableComponent
 *
 * - WoundableComponent:
 *  A thing that can receive woundsm, in practice this is usually a body part or
 *  an organ, at the time of writing its only body parts i.e. limbs.
 *
 * - WoundComponent: (!!! ITS A DIFFERENT THING FROM WOUNDABLECOMPONENT !!!)
 *  a wound entity stored in a woundables wounds container. One wound normally
 *  corresponds to one damage type proto, such as blunt, slash, piercing,
 *  radiation etc. the wound tracks WoundSeverityPoint, WoundSeverity,
 *  DamageType, DamageGroup, and mangle thing for ui and shit
 *
 * - WoundSeverityPoint:
 *  the fixedpoint damage/severity ammount on one wound, higher means worse
 *  this is what healing subtracts from and damage adds to
 *
 * - WoundSeverity:
 *  thresholds are in WoundSystem.Data.cs:
 *  Healed 0
 *  Minor 1
 *  Moderate 25
 *  Severe 50
 *  Critical 80
 *  Loss 100.
 *  These are scaled by the holding woundables IntegrityCap / 100, so a part with 50 max cap
 *  reaches the threshholds twice as fast (severe is at 25 instead of 50 etc)
 *
 *
 * yaml
 * - wound prototypes live in Resources/Prototypes/_Shitmed/Entities/Surgery/wounds.yml
 *  they are normal entities with WoundComponent some also have
 *  TraumaInflicterComponent, BleedInflicterComponent and others
 *
 * - damage type ids are also shitcoded as wound prototype ids.
 *  for example, taking slash damage tries to create or continue the wound prototype named "Slash"
 *  if theres no entity prototype with that id and WoundComponent, no wound is made
 *
 * - every woundable starts as its own RootWoundable on init
 * - woundables can be parented when organs/parts are inserted into other parts
 *  ParentWoundable points upward, ChildWoundables points downward, and RootWoundable is something scary idk
 *
 *
 *
 * API
 *  GetAllWoundableChildren returns children recursively and then the target itself
 *  - example: `foreach (var child in GetAllWoundableChildren(part, woundable)) {}`
 *
 *  GetAllWounds walks that hierarchy and returns wounds on every child woundable
 *  - example: `foreach (var wound in GetAllWounds(part, woundable)) {}`
 *
 *  GetWoundableWounds returns only wounds directly on the requested woundable
 *  - example: `var directWounds = GetWoundableWounds(part, woundable);`
 *
 *  GetWoundableWoundsWithComp returns direct wounds, use this for bleed/trauma/etc wound component shittery
 *  - example: `var wound = GetWoundableWoundsWithComp<BleedInflicterComponent>(part, woundable);`
 *
 *  GetAllWoundableChildrenWithComp its magic.
 *  - example: https://discord.com/channels/1447652269758746656/1494665336862146691/1524833338622480434
 *
 *  AddWoundableToParent wires a child woundable into a parent woundable hierarchy and raises wound-added events for its wounds
 *  - example: `if (AddWoundableToParent(parentPart, childPart, parentWoundable, childWoundable)) {}`
 *
 *  RemoveWoundableFromParent detaches a child woundable from its parent hierarchy and raises events events for its wounds to remove em
 *  - example: `if (RemoveWoundableFromParent(parentPart, childPart, parentWoundable, childWoundable)) {}`
 *
 *  HasWoundsExceedingMangleSeverity checks direct wounds for MangleSeverity and is used before applying mangled trauma behavior
 *  - example: `if (HasWoundsExceedingMangleSeverity(part, woundable))`
 *
 *  GetWoundableSeverityPoint sums WoundSeverityPoint on direct wounds, optionally filtered by damage group and healabillity <--------------------- very useful btw
 *  - example: `var bruteSeverity = GetWoundableSeverityPoint(part, woundable, "Brute", true);`
 *
 *  GetWoundableIntegrityDamage returns the same direct wound severity sum for integrity damage shit, with the same filters
 *  - example: `var damage = GetWoundableIntegrityDamage(part, woundable, "Brute", true);`
 *
 *  AddWound is serveronly, requires AllowWounds, adds wounds (shocking), massive mispredict issues due to it being serveronly
 *  - example: i got lazy of putting more examples figure it out by yourself bru use ur IDEs search, everything neat has been example'd above
 *
 *  SetWoundSeverity sets the wound to a specific severity
 *  ApplyWoundSeverity does fucking SOMETHING IDFK applies x to y ?????
 *
 *  ApplySeverityModifiers fuck up with multiplier change values and multiplies incoming positive severity by that value
 *  - look at WoundableComponent.SeverityMultipliers
 *  - doing anything to a severity multiplier rechecks wound severity
 *
 *  UpdateWoundableIntegrity sums all wounds in the Wounds container and sets WoundableIntegrity to IntegrityCap minus that value ????
 *
 *  TryHealWoundsOnWoundable can heal by damage group, damage type, or DamageSpecifier
 *  - it splits the requested ammount across matching healable wounds
 *  ForceHealWoundsOnWoundable removes matching wounds directly and ignores normal per wound healing shit
 *  TryHealWoundsOnOwner gathers all woundables and wounds on a body and splits a DamageSpecifier across wounds of matching damage types
 *  Rejuvenate does guess what.
 *
 *  CanHealWound - check line ~147 of this system
 *
 * Wounds logic
 * - DamageableSystem raises DamageDealtEvent on the woundable
 * - positive damage:
 *  1: if AllowWounds is false, nothing happens
 *  2: the damage type is checked as a valid wound prototype
 *  3: TryInduceWound first tries TryContinueWound
 *  4: If wound with the same id already exists, severity is added to it
 *  5: otherwise TryCreateWound spawns the wound prototype and AddWound inserts it.
 *
 * - negative damage:
 *  negative damage is treated as healing.
 *  it calls TryHealWoundsOnWoundable for the matching damage type, unless blockers prevent healing
 *
 * - DamageSetEvent:
 *  rebuilds wound state from set damage by inducing wounds for the existing damage types
 *  this is used when damage is set dirrectly
 *
 *
 *
 * healing
 * - WoundSystem.Update runs at _medicalHealingTickrate which is 5 seconds (unless changed)
 *  it skips ents with no organ container and recently damaged bodies
 *  where LastModifiedTime is newer than _minimumTimeBeforeHeal, and incapacitated ents
 *  that also means that if you cuff someone they wont heal on their own lmao
 * - each woundable with CanHealDamage or CanHealBleeds gets queued as a WoundJob (no erp)
 * - CanHealDamage is true when integrity is above DamageThreshold but below cap
 * - CanHealBleeds is true when Bleeds is above 0 but below BleedsThreshold
 * - Passive damage healing splits HealAbility across all healable wounds on that
 *  woundable, does multipliers, then sends negative damage through DamageableSystem
 *  targeted at the part, that negative damage comes back through
 *  DamageDealtEvent and reduces the matching wound severities
 *
 * healing blocking
 * - CanHealWound checks WoundComponent.CanBeHealed unless ignoreBlockers is true
 * - it raises WoundHealAttemptOnWoundableEvent on the holding woundable and WoundHealAttemptEvent on the wound
 * - severed woundables cancel WoundHealAttemptOnWoundableEvent
 * - BleedInflicterComponent cancels WoundHealAttemptEvent while the wound is bleeding, unless IgnoreBlockers is true (i.e. chems i guess)
 *  this means bleeding wounds must usually be stopped before normal wound severity healing works
 *
 *
 *
 * bleeding
 * - bleeding is not on every wound, it only exists on wound protos with BleedInflicterComponent, like Slash/Piercing, see line 80-ish
 * - when such a wound is added, BodyBloodstreamSystem sets BleedingAmountRaw from WoundSeverityPoint * bleeding severity cvar
 *  sets scaling timestamps, and marks the wound bleeding if it passes SeverityThreshold and CanBleed thing
 * - BleedingAmount is BleedingAmountRaw * scaling
 * - BleedingModifiers can surpress or allow bleeding, the highest priority modifier wins
 * - on wound severity increases, BleedInflicterComponent regoids raw bleeding, may do bleeding again, and resets scaling
 * - TryHealBleedingWounds lowers BleedingAmountRaw or stops bleeding entirely
 * - TryHaltAllBleeding only flips IsBleeding to false on bleed inflicter wounds, with force it also marks wounds CanBeHealed
 *  so normally blocked wound types can be healed
 * - BleedRemoverComponent, used by heat wounds, can reduce bleeding when severity changes
 *
 *
 *
 * surgery
 * - surgeries that tend wounds steps call into wound healing and bleed treatment APIs
 * - tend wound healing can pass ignoreBlockers or use damage group filtering, which
 *  is why some surgery paths can heal wounds that ordinary passive healing cannot (by design)
 * - bleed treatment surgery edits BleedInflicterComponent directly, reducing scaling and BleedingAmountRaw and
 *  clearing IsBleeding when enough has been treated.
 *
 * trauma / traumas
 * - (some) wound protos have TraumaInflicterComponent
 * - WoundSeverityPointChangedEvent is what traumasys uses to decide whether to change traumas from a wound
 * - if a wound reaches its MangleSeverity and the part has wounds exceeding that threshold, ApplyWoundSeverity asks traumasys to apply mangled traumas
 * - some traumas block wound removal after the wound itself is healed????
 *
 * - AmputateWoundable removes the woundable from the body, marks it as Severed, applies DamageOnAmputate to the parent
 *  transfers/updates parent wound behavior, forces parent wounds to bleed harder, and goids the part to the server
 * - AmputateWoundableSafely only removes the organ/body part and marks it Severed
 * - DestroyWoundable marks the limb Severed, updates body status, adds a dismemberment wound/trauma and heavy bleeding to the parent, gibs/destroys the
 *   target (limb), and if the root woundable is destroyed it gibs the guy
 * - DecapitateEvent routes to AmputateWoundable for the head when possible
 *
 *
 *
 * events
 * - WoundAddedEvent / WoundAddedOnBodyEvent
 *  raised when a wound is added duh
 *  those two get ran simultaniuoulsy most of the time, only difference is that WoundAddedOnBodyEvent gets ran on the root body
 * - WoundRemovedEvent
 *  raised when a wound is gone
 * - WoundSeverityPointChangedEvent
 *  raised when the wound VALUE severity changes, i.e. integrity change from 11 -> 12.6
 * - WoundSeverityChangedEvent
 *  raised when the named wound severity changes, i.e. severity change from minor -> severe
 * - WoundableIntegrityChangedEvent (!!! WOUNDS AND WOUNDABLES ARE DIFFERENT THINGS !!!)
 *  raised when woundABLE integrity changes
 * - WoundableSeverityChangedEvent
 *  raised when the named woundABLE severity changes
 * - WoundHealAttemptEvent / WoundHealAttemptOnWoundableEvent
 *  raised to allow wounds and woundables to block healing (?)
 * - DecapitateEvent
 *  raised when an entity is decapitated
 *
 *
 * PSA
 * - most wound creation is serveronly so expect misspredicts
 *  dont expect client to call AddWound to reproduce real wound state for whatever fucking reason todo inkymed
 * - the wound protoid must match the damage type id for wound creation
 *  if you have a new damage type (LOOKING AT YOU ARMOK) its name should be 1:1 to the wound type or else
 *  it will not work (unless you shitcode it)
 * - bleeding wounds block normal healing unless ignoreBlockers is used or bleeding is stopped first
 * - scars are gone todo inkymed
 * - WoundableSeverity is based on remaining integrity and not the count of wounds
 * - a healed wound may remain in the container if it has blocking traumas for WHATEVER FUCKING REASON?????
 * - child woundables exist, learn a difference between GetAllWounds and GetWoundableWounds
 * - By reading this in its entirety you are depositing your soul into the national
 *  shitcoders charity of woundmed victims, and you are officially on the fifth layer of hell.
 *                                              - xoxo Lucifer (ironic)
 *
 *
 */ // /inkymed
public sealed partial class WoundSystem : EntitySystem
{
    [Dependency] private EntityQuery<WoundComponent> _query = default!;
    [Dependency] private EntityQuery<WoundableComponent> _woundableQuery = default!;

    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private BodySystem _body = default!;
    [Dependency] private BodyPartSystem _part = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _mobThreshold = default!;

    // I'm the one.... who throws........
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private TraumaSystem _trauma = default!;

    private float _medicalHealingTickrate = 5f;
    private TimeSpan _nextUpdate;
    private TimeSpan _minimumTimeBeforeHeal = TimeSpan.FromSeconds(2f);

    private const double WoundJobTime = 0.005;
    private readonly JobQueue _woundJobQueue = new(WoundJobTime);

    public sealed class WoundJob : Job<object>
    {
        private readonly WoundSystem _self;
        private readonly Entity<WoundableComponent> _ent;
        private readonly EntityUid _bodyEnt;
        public WoundJob(WoundSystem self, Entity<WoundableComponent> ent, EntityUid bodyEnt, double maxTime, CancellationToken cancellation = default) : base(maxTime, cancellation)
        {
            _self = self;
            _ent = ent;
            _bodyEnt = bodyEnt;
        }

        public WoundJob(WoundSystem self, Entity<WoundableComponent> ent, EntityUid bodyEnt, double maxTime, IStopwatch stopwatch, CancellationToken cancellation = default) : base(maxTime, stopwatch, cancellation)
        {
            _self = self;
            _ent = ent;
            _bodyEnt = bodyEnt;
        }

        protected override Task<object?> Process()
        {
            _self.ProcessHealing(_ent, _bodyEnt);

            return Task.FromResult<object?>(null);
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoundComponent, ComponentGetState>(OnWoundComponentGet);
        SubscribeLocalEvent<WoundComponent, ComponentHandleState>(OnWoundComponentHandleState);
        SubscribeLocalEvent<WoundableComponent, ComponentGetState>(OnWoundableComponentGet);
        SubscribeLocalEvent<WoundableComponent, ComponentHandleState>(OnWoundableComponentHandleState);
        InitWounding();
        InitializeHealing();
        // inkymed
        InitInky();
        // /inky

        Subs.CVar(_cfg, SurgeryCVars.MedicalHealingTickrate, val => _medicalHealingTickrate = val, true);
        Subs.CVar(_cfg, SurgeryCVars.MinimumTimeBeforeHeal, val => _minimumTimeBeforeHeal = TimeSpan.FromSeconds(val), true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _woundJobQueue.Process();

        if (!_timing.IsFirstTimePredicted)
            return;

        var now = _timing.CurTime;
        if (now < _nextUpdate)
            return;

        _nextUpdate = now + TimeSpan.FromSeconds(1f / _medicalHealingTickrate);

        // TODO: make a marker component for alive mobs with a body
        var query = EntityQueryEnumerator<BodyComponent, DamageableComponent>();
        while (query.MoveNext(out var ent, out var body, out var damageable))
        {
            if (body.Organs is not {} organs ||
                TerminatingOrDeleted(ent) ||
                now - damageable.LastModifiedTime < _minimumTimeBeforeHeal ||
                _mobState.IsIncapacitated(ent))
                continue;

            foreach (var organ in organs.ContainedEntities)
            {
                if (!_woundableQuery.TryComp(organ, out var woundable))
                    continue;

                if (woundable.CanHealDamage || woundable.CanHealBleeds)
                    _woundJobQueue.EnqueueJob(new WoundJob(this, (organ, woundable), ent, WoundJobTime));
            }
        }
    }

    private void ProcessHealing(Entity<WoundableComponent> woundable, EntityUid bodyEnt)
    {
        if (woundable.Comp.CanHealBleeds)
            TryHealBleedingWounds(woundable, (float) -woundable.Comp.BleedingTreatmentAbility, out _, woundable);

        if (!woundable.Comp.CanHealDamage)
            return;

        var woundsToHeal = GetWoundableWounds(woundable)
            .Where(wound => CanHealWound(wound, wound))
            .ToList();

        var healAmount = -woundable.Comp.HealAbility / woundsToHeal.Count;
        var damageSpecifier = new DamageSpecifier();
        var anythingToHeal = false;
        foreach (var wound in woundsToHeal)
        {
            if (wound.Comp.SelfHealMultiplier <= 0)
                continue;

            var damageType = wound.Comp.DamageType;
            var adjustedHealAmount = ApplyHealingRateMultipliers(wound, woundable, healAmount);

            if (adjustedHealAmount != 0)
                anythingToHeal = true;

            if (damageSpecifier.DamageDict.TryGetValue(damageType, out var existingAmount))
                damageSpecifier.DamageDict[damageType] = existingAmount + adjustedHealAmount;
            else
                damageSpecifier.DamageDict.TryAdd(damageType, adjustedHealAmount);
        }

        if (!anythingToHeal || !TryComp<BodyPartComponent>(woundable, out var part))
            return;

        _damageable.TryChangeDamage(bodyEnt,
            damageSpecifier,
            ignoreResistances: false,
            targetPart: _body.GetTargetBodyPart(part.PartType, part.Symmetry));
    }

    private void OnWoundComponentGet(EntityUid uid, WoundComponent comp, ref ComponentGetState args)
    {
        var state = new WoundComponentState
        {
            HoldingWoundable =
                TryGetNetEntity(comp.HoldingWoundable, out var holdingWoundable)
                    ? holdingWoundable.Value
                    : NetEntity.Invalid,

            WoundSeverityPoint = comp.WoundSeverityPoint,

            WoundType = comp.WoundType,

            DamageGroup = comp.DamageGroup,
            DamageType = comp.DamageType,

            // inkymed
            /*
            ScarWound = comp.ScarWound,
            IsScar = comp.IsScar,
            */
            // /inkymed

            WoundSeverity = comp.WoundSeverity,

            WoundVisibility = comp.WoundVisibility,

            CanBeHealed = comp.CanBeHealed,
            SelfHealMultiplier = comp.SelfHealMultiplier
        };

        args.State = state;
    }

    private void OnWoundComponentHandleState(EntityUid uid, WoundComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not WoundComponentState state)
            return;

        // Predict events on client!!
        // TODO SHITMED: dont fucking need this? container events are applied in prediction
        var holdingWoundable = TryGetEntity(state.HoldingWoundable, out var e) ? e.Value : EntityUid.Invalid;
        if (holdingWoundable != component.HoldingWoundable)
        {
            component.HoldingWoundable = holdingWoundable;

            if (holdingWoundable == EntityUid.Invalid)
            {
                if (TryComp(holdingWoundable, out WoundableComponent? oldParentWoundable) &&
                    TryComp(oldParentWoundable.RootWoundable, out WoundableComponent? oldWoundableRoot))
                {
                    var ev2 = new WoundRemovedEvent(component, oldParentWoundable, oldWoundableRoot);
                    RaiseLocalEvent(component.HoldingWoundable, ref ev2);
                }
            }
            else
            {
                if (TryComp(holdingWoundable, out WoundableComponent? parentWoundable) &&
                    TryComp(parentWoundable.RootWoundable, out WoundableComponent? woundableRoot))
                {
                    var ev = new WoundAddedEvent(component, parentWoundable, woundableRoot);
                    RaiseLocalEvent(uid, ref ev);
                    RaiseLocalEvent(holdingWoundable, ref ev);

                    if (_body.GetBody(holdingWoundable) is {} body)
                    {
                        var bodyEv = new WoundAddedOnBodyEvent((uid, component), parentWoundable, woundableRoot);
                        RaiseLocalEvent(body, ref bodyEv);
                    }
                }
            }
        }

        if (component.WoundSeverityPoint != state.WoundSeverityPoint)
        {
            var ev = new WoundSeverityPointChangedEvent(component,
                component.WoundSeverityPoint,
                state.WoundSeverityPoint);
            RaiseLocalEvent(uid, ref ev);

            // TODO: On body changed events aren't predicted, welp
        }

        component.WoundSeverityPoint = state.WoundSeverityPoint;

        if (component.HoldingWoundable != EntityUid.Invalid)
        {
            UpdateWoundableIntegrity(component.HoldingWoundable);
            CheckWoundableSeverityThresholds(component.HoldingWoundable);
        }

        component.WoundType = state.WoundType;

        component.DamageGroup = state.DamageGroup;
        if (state.DamageType != null)
            component.DamageType = state.DamageType;

        // inkymed
        /*
        component.ScarWound = state.ScarWound;
        component.IsScar = state.IsScar;
        */
        // /inkymed

        if (component.WoundSeverity != state.WoundSeverity)
        {
            var ev = new WoundSeverityChangedEvent(component.WoundSeverity, state.WoundSeverity);
            RaiseLocalEvent(uid, ref ev);
        }

        component.WoundSeverity = state.WoundSeverity;
        component.WoundVisibility = state.WoundVisibility;
        component.CanBeHealed = state.CanBeHealed;
        component.SelfHealMultiplier = state.SelfHealMultiplier;
    }

    private void OnWoundableComponentGet(EntityUid uid, WoundableComponent comp, ref ComponentGetState args)
    {
        var state = new WoundableComponentState
        {
            ParentWoundable = TryGetNetEntity(comp.ParentWoundable, out var parentWoundable) ? parentWoundable : null,
            RootWoundable = TryGetNetEntity(comp.RootWoundable, out var rootWoundable)
                ? rootWoundable.Value
                : NetEntity.Invalid,

            ChildWoundables =
                comp.ChildWoundables
                    .Select(woundable => TryGetNetEntity(woundable, out var ne)
                        ? ne.Value
                        : NetEntity.Invalid)
                    .ToHashSet(),
            // Attached and Detached -Woundable events are handled on client with containers

            AllowWounds = comp.AllowWounds,
            CanRemove = comp.CanRemove,
            CanBleed = comp.CanBleed,

            DamageContainerID = comp.DamageContainerID,

            DodgeChance = comp.DodgeChance,
            Bleeds = comp.Bleeds,
            WoundableIntegrity = comp.WoundableIntegrity,
            HealAbility = comp.HealAbility,

            SeverityMultipliers =
                comp.SeverityMultipliers
                    .Select(multiplier
                        => (TryGetNetEntity(multiplier.Key, out var ne) ? ne.Value : NetEntity.Invalid,
                            multiplier.Value))
                    .ToDictionary(),
            HealingMultipliers =
                comp.HealingMultipliers
                    .Select(multiplier
                        => (TryGetNetEntity(multiplier.Key, out var ne) ? ne.Value : NetEntity.Invalid,
                            multiplier.Value))
                    .ToDictionary(),

            WoundableSeverity = comp.WoundableSeverity,
        };

        args.State = state;
    }

    private void OnWoundableComponentHandleState(EntityUid uid, WoundableComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not WoundableComponentState state)
            return;

        TryGetEntity(state.ParentWoundable, out component.ParentWoundable);
        TryGetEntity(state.RootWoundable, out var rootWoundable);
        component.RootWoundable = rootWoundable ?? EntityUid.Invalid;

        component.ChildWoundables = state.ChildWoundables
            .Select(x => TryGetEntity(x, out var y) ? y.Value : EntityUid.Invalid)
            .Where(x => x.Valid)
            .ToHashSet();
        // Attached and Detached -Woundable events are handled on client with containers

        component.AllowWounds = state.AllowWounds;
        component.CanRemove = state.CanRemove;
        component.CanBleed = state.CanBleed;

        component.DamageContainerID = state.DamageContainerID;

        component.DodgeChance = state.DodgeChance;
        component.HealAbility = state.HealAbility;
        component.Bleeds = state.Bleeds;

        component.SeverityMultipliers =
            state.SeverityMultipliers
                .Select(multiplier
                    => (TryGetEntity(multiplier.Key, out var ne) ? ne.Value : EntityUid.Invalid, multiplier.Value))
                .ToDictionary();
        component.HealingMultipliers =
            state.HealingMultipliers
                .Select(multiplier
                    => (TryGetEntity(multiplier.Key, out var ne) ? ne.Value : EntityUid.Invalid, multiplier.Value))
                .ToDictionary();

        if (component.WoundableIntegrity != state.WoundableIntegrity)
        {
            var ev = new WoundableIntegrityChangedEvent(component.WoundableIntegrity, state.WoundableIntegrity);
            RaiseLocalEvent(uid, ref ev);

            if (_body.GetBody(uid) is {} body)
                UpdateMobAlerts(body);
        }

        component.WoundableIntegrity = state.WoundableIntegrity;

        if (component.WoundableSeverity != state.WoundableSeverity)
        {
            var ev = new WoundableSeverityChangedEvent(component.WoundableSeverity, state.WoundableSeverity);
            RaiseLocalEvent(uid, ref ev);
        }
        component.WoundableSeverity = state.WoundableSeverity;
    }

    private void UpdateMobAlerts(EntityUid body)
    {
        if (TryComp<MobStateComponent>(body, out var mob))
            _mobThreshold.UpdateAlerts(body, mob.CurrentState);
    }
}
