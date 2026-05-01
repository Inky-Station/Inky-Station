using Content.Inky.Shared.Werewolf.Components;
using Content.Shared.Body.Components;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;

namespace Content.Inky.Shared.Werewolf.Systems;

public partial class SharedWerewolfBasicAbilitiesSystem
{
    public void InitializeWerewolfDire()
    {
        SubscribeLocalEvent<WerewolfBasicAbilitiesComponent, EventWerewolfBleedingBite>(TryBite);
        SubscribeLocalEvent<WerewolfBasicAbilitiesComponent, WerewolfBleedingBiteDoAfterEvent>(DoBite);
    }

    private void TryBite(EntityUid uid, WerewolfBasicAbilitiesComponent component, EventWerewolfBleedingBite args)
    {
        if (TryComp<MobStateComponent>(args.Target, out var mobState) && mobState.CurrentState == MobState.Dead) // to prevent wolves from biting corpses for heals and whatnot
        {
            _popup.PopupEntity(Loc.GetString("werewolf-bite-fail-state"), uid, uid, PopupType.Large);
            return;
        }

        _popup.PopupEntity(Loc.GetString("werewolf-bite-start", ("user", uid), ("target", args.Target)), uid, uid, PopupType.LargeCaution); // todo locale

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(1), new WerewolfBleedingBiteDoAfterEvent(), uid, args.Target)
        {
            DistanceThreshold = 1.5f,
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        });

        args.Handled = true;
    }

    private void DoBite(EntityUid uid, WerewolfBasicAbilitiesComponent comp, WerewolfBleedingBiteDoAfterEvent args)
    {
        if (args.Cancelled || args.Target == null)
            return;

        SpillBloodPercentage(args.Target.Value, 30); // todo werewolf unhardcode
        TryRegen(uid, comp, new EventWerewolfRegen()); // goida

        args.Handled = true;
    }

    private void SpillBloodPercentage(EntityUid uid, int percentage) // if you make the number be negative or above 100 i will be very sad.
    {
        if (!TryComp<BloodstreamComponent>(uid, out var stream))
            return;

        if (!_solution.ResolveSolution(uid, stream.BloodSolutionName, ref stream.BloodSolution, out var solution))
            return;

        var blood = _solution.SplitSolution(stream.BloodSolution.Value, solution.Volume * (percentage / 100f));

        if (blood.Volume > FixedPoint2.Zero)
            _puddle.TrySpillAt(uid, blood, out _);
    }
}
