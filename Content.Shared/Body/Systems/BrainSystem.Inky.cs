using Content.Inky.Common.Medical;
using Content.Shared._Shitcod;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects.Effects.Body;
using Content.Shared.Metabolism;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Systems;

public sealed partial class BrainSystem
{
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(4); // full cycle of inhaling/exhaling is 4 seconds so...
    private static readonly ProtoId<MetabolismStagePrototype> RespirationStage = new("Respiration"); // no fucking idea what this is
    private TimeSpan _nextUpdate;

    private void InitializeInky()
    {
        _nextUpdate = _timing.CurTime + UpdateInterval;
        SubscribeLocalEvent<AutismComponent, MapInitEvent>((ent, ref args) => UpdateBrainAlert(ent.Owner, brain => brain.AutismAlert, true));
        SubscribeLocalEvent<AutismComponent, ComponentShutdown>((ent, ref args) => UpdateBrainAlert(ent.Owner, brain => brain.AutismAlert, false));

        SubscribeLocalEvent<LobotomisedComponent, MapInitEvent>((ent, ref args) => UpdateBrainAlert(ent.Owner, brain => brain.LobotomyAlert, true));
        SubscribeLocalEvent<LobotomisedComponent, ComponentShutdown>((ent, ref args) => UpdateBrainAlert(ent.Owner, brain => brain.LobotomyAlert, false));

        SubscribeLocalEvent<BodyComponent, SaturateBrainEvent>(_body.RelayEvent);
        SubscribeLocalEvent<BrainComponent, BodyRelayedEvent<SaturateBrainEvent>>(OnSaturateBrain);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        var eqe = EntityQueryEnumerator<BrainComponent>();
        while (eqe.MoveNext(out var uid, out var brain))
        {
            brain.AirSaturation = Math.Clamp(brain.AirSaturation - brain.AirConsumption, 0f, 1f);
            Dirty(uid, brain);
        }
    }

    private void OnSaturateBrain(Entity<BrainComponent> ent, ref BodyRelayedEvent<SaturateBrainEvent> args)
    {
        if (!TryComp<MetabolizerComponent>(args.Args.Lung, out var metabolizer))
            return;

        var saturation = GetSaturation(args.Args.Gas, (args.Args.Lung, metabolizer)) * ent.Comp.AirSaturationGain;
        if (saturation == 0f)
            return;

        ent.Comp.AirSaturation = Math.Clamp(ent.Comp.AirSaturation + saturation, 0f, 1f);
        Dirty(ent);
    }

    private float GetSaturation(GasMixture gas, Entity<MetabolizerComponent> lung)
    {
        var saturation = 0f;
        foreach (var gasId in Enum.GetValues<Gas>()) // atmos is so scary
        {
            var moles = gas[(int) gasId];
            if (moles <= 0f)
                continue;

            var atmosreagent = _ilya.GasReagents[(int) gasId];
            if (atmosreagent is null)
                continue;

            // atmos gases are reagents, which is even more cursed than reagents on their own
            // idfk copied from respiratorysystem
            var reagent = ProtoMan.Index<ReagentPrototype>(atmosreagent);
            if (reagent.Metabolisms == null)
                continue;

            if (reagent.Metabolisms.Metabolisms.TryGetValue(RespirationStage, out var entry) != true)
                continue;

            var ammount = MathF.Min(moles * Atmospherics.BreathMolesToReagentMultiplier, 15f);

            // ok so, metabolizer bullshit
            // due to gasses being reagent-bs^2, the reagents have oxygenate property (aka nitrogen)
            // and we check that shit in case the dude breathes piss instead of air or whatever
            if (entry.Effects is null)
                continue;

            foreach (var effect in entry.Effects)
            {
                if (effect is Oxygenate oxygenate && _entcond.TryConditions(lung, oxygenate.Conditions))
                    saturation += oxygenate.Factor * ammount;
            }
        }

        return saturation;
    }

    # region statuses
    private void UpdateBrainAlert(
        EntityUid uid,
        Func<BrainComponent, ProtoId<AlertPrototype>?> getAlert, // i am so fucking scared of words
        bool enabled)
    {
        if (TryComp<BodyComponent>(uid, out var bodycomp))
            DoBody(uid, bodycomp);

        if (!_organQ.TryComp(uid, out var organ)
            || organ.Body is not { } body
            || !_brainQ.TryComp(uid, out var brain)
            || getAlert(brain) is not { } alert)
            return;

        if (enabled)
            _alerts.ShowAlert(body, alert);
        else
            _alerts.ClearAlert(body, alert);
    }

    private void DoBody(EntityUid uid, BodyComponent body)
    {
        foreach (var brain in _body.GetOrgans<BrainComponent>((uid, body)))
        {
            EnsureComp<AutismComponent>(brain.Owner); // idfk about lobotomy since its broken, todo inky fixme
        }
    }
    #endregion
}
