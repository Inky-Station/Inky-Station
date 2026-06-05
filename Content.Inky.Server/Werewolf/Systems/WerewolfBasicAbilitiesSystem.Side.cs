using System.Linq;
using Content.Goobstation.Shared.Changeling.Components;
using Content.Inky.Shared.Werewolf;
using Content.Inky.Shared.Werewolf.Components;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Inky.Server.Werewolf.Systems;

/// <summary>
/// Handles side abilities and helpers for the werewolf
/// </summary>
public sealed partial class WerewolfBasicAbilitiesSystem
{
    public void InitializeWerewolfSide()
    {
        SubscribeLocalEvent<WerewolfBasicAbilitiesComponent, EventWerewolfDevour>(TryDevour);
        SubscribeLocalEvent<WerewolfBasicAbilitiesComponent, WerewolfDevourDoAfterEvent>(DoDevour);
        SubscribeLocalEvent<WerewolfBasicAbilitiesComponent, EventWerewolfGut>(TryGut);
        SubscribeLocalEvent<WerewolfBasicAbilitiesComponent, WerewolfGutDoAfterEvent>(DoGut);
    }
    # region devour
    private void TryDevour(EntityUid uid, WerewolfBasicAbilitiesComponent component, EventWerewolfDevour args)
    {
        if (component.Transfurmed != true)
        {
            _popup.PopupPredictedCursor(Loc.GetString("werewolf-action-fail-transfurmed"), uid);
            return;
        }

        var target = args.Target;

        if (HasComp<WerewolfBitComponent>(target))
        {
            _popup.PopupPredictedCursor(Loc.GetString("werewolf-devour-fail-devoured"), uid);
            return;
        }
        if (!HasComp<AbsorbableComponent>(target)) // i mean... it works? also less wizden files changes
        {
            _popup.PopupPredicted(Loc.GetString("changeling-absorb-fail-unabsorbable"), uid, uid);
            return;
        }

        var popupOthers = Loc.GetString("werewolf-devour-start", ("user", uid), ("target", target));
        _popup.PopupPredicted(popupOthers, uid, uid, PopupType.LargeCaution);

        var dargs = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(4), new WerewolfDevourDoAfterEvent(), uid, target) // todo werewolf unhardcode duration
        {
            DistanceThreshold = 1.5f,
            BreakOnDamage = true,
            BreakOnHandChange = false,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd,
            MultiplyDelay = false,
        };
        _doAfter.TryStartDoAfter(dargs);
    }

    public ProtoId<DamageGroupPrototype> DevourDamage = "Brute"; // bro
    private void DoDevour(EntityUid uid, WerewolfBasicAbilitiesComponent comp, WerewolfDevourDoAfterEvent args)
    {
        if (args.Args.Target == null)
            return;

        var target = args.Args.Target.Value;

        if (args.Cancelled
            || HasComp<WerewolfBitComponent>(target)
            || !TryComp<BodyComponent>(target, out var body))
            return;

        var dmg = new DamageSpecifier(_proto.Index(DevourDamage), 35); // todo werewolf unhardcode this
        _damage.TryChangeDamage(target, dmg, true, true);
        RipLimb(target, body);

        EnsureComp<WerewolfBitComponent>(target);

        if (!_mind.TryGetMind(uid, out var mindId, out _)
            || !TryComp<WerewolfMindComponent>(mindId, out var mindComp))
            return;

        mindComp.Currency += comp.AmountDevour;
        mindComp.BittenPeople.Add(args.Args.Target.Value);

        _hunger.ModifyHunger(uid, +80); // todo werewolf maybe put as a var inside a comp or sdome shit
        _audio.PlayPvs(comp.RipSound, uid);
    }

    private void TryGut(EntityUid uid, WerewolfBasicAbilitiesComponent comp, EventWerewolfGut args)
    {
        var target = args.Target;

        if (!HasComp<AbsorbableComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("changeling-absorb-fail-unabsorbable"), uid, uid);
            return;
        }

        _mind.TryGetMind(target, out var mindId, out var mind);

        if (mind == null)
        {
            // _popup.PopupEntity(Loc.GetString("werewolf-gut-fail-mind"), uid, uid); // todo werewolf locale
            // return; // todo werewolf uncomment before release
        }

        var popupOthers = Loc.GetString("werewolf-gut-start", ("user", uid), ("target", target)); // todo locale
        _popup.PopupPredicted(popupOthers, uid, uid, PopupType.LargeCaution);

        var dargs = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(4), new WerewolfGutDoAfterEvent(), uid, target)// todo werewolf unhardcode duration
        {
            DistanceThreshold = 1.5f,
            BreakOnDamage = true,
            BreakOnHandChange = false,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd,
            MultiplyDelay = false,
        };
        _doAfter.TryStartDoAfter(dargs);
    }

    #endregion
    #region helpers
    private void DoGut(EntityUid uid, WerewolfBasicAbilitiesComponent comp, WerewolfGutDoAfterEvent args)
    {
        if (args.Args.Target == null)
            return;

        var target = args.Args.Target.Value;

        if (args.Cancelled
            || !TryComp<BodyComponent>(target, out var body))
            return;

        if (!TryRemoveOrgan(uid, target, out var removedOrgan))
            return;

        _blood.SpillAllSolutions(target);
        if (_mind.TryGetMind(uid, out var mindId, out _) && TryComp<WerewolfMindComponent>(mindId, out var mindComp))
            mindComp.Currency += comp.AmountGut;

        _hunger.ModifyHunger(uid, +20); // todo werewolf maybe put this inside comp
        _audio.PlayPvs(comp.RipSound, uid);
    }

    private bool TryRemoveOrgan(EntityUid user, EntityUid target, out EntityUid? removedOrgan) // shit was originally taken from devil shitcode but upstream broke a shitton of stuff
    {
        removedOrgan = null;

        if (!TryComp<BodyComponent>(target, out var body))
            return false;

        var organs = _body.GetInternalOrgans((target, body))
            .Where(organ => !HasComp<BrainComponent>(organ.Owner))
            .ToList();

        if (organs.Count < 1)
        {
            _popup.PopupEntity(Loc.GetString("werewolf-gut-no-organs-left"), user, user);
            return false;
        }

        var nextOrgan = _gambling.Next(organs.Count); // idk
        var picked = organs[nextOrgan];
        removedOrgan = picked.Owner;

        if (TryComp<OrganComponent>(picked.Owner, out var organComp))
            _body.RemoveOrgan((target, body), new Entity<OrganComponent?>(picked.Owner, organComp)); // this is horrible
        QueueDel(picked.Owner);

        _popup.PopupEntity(Loc.GetString("werewolf-gut-success"), user, user);

        return true;
    }

    private void RipLimb(EntityUid target, BodyComponent body)
    {
        var allOrgans = _body.GetOrgans((target, body));
        var limbs = allOrgans // limbs are considered organs for some reason
            .Where(organ =>
            {
                var category = _body.GetCategory(new Entity<OrganComponent?>(organ.Owner, organ.Comp));
                return category == "ArmLeft" || category == "ArmRight"; // TODO WEREWOLF: DESHITCODE
            })// i have PTSD from shitmed and inkymed looking at this shit above
            .ToList();

        if (limbs.Count <= 0)
            return;

        var nextOrgan = _gambling.Next(limbs.Count); // boo copypaste from TryRemoveOrgan
        var picked = limbs[nextOrgan];

        if (!TryComp<WoundableComponent>(picked.Owner, out var woundable)
            || !woundable.ParentWoundable.HasValue)
            return;

        _wound.AmputateWoundableSafely(woundable.ParentWoundable.Value, picked.Owner, woundable);
    }
    # endregion
}
