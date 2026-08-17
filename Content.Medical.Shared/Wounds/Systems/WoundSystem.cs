// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Body;
using Content.Medical.Shared.Traumas;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
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
 * also it may be outdated rn
 * by a lot
 */ // /inkymed
public sealed partial class WoundSystem : EntitySystem
{
    [Dependency] private EntityQuery<BleedInflicterComponent> _bleedQuery = default!;
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

    // I'm the one.... who throws........
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private TraumaSystem _trauma = default!;

    private CompName _woundName;

    public override void Initialize()
    {
        base.Initialize();
        // inkymed
        InitInky();
        // /inky

        _woundName = Factory.CompName<WoundComponent>();
    }
}
