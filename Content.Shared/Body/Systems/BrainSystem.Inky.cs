using Content.Inky.Common.CCVar;
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
using Content.Shared.Rejuvenate;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Systems;

public sealed partial class BrainSystem
{
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(4); // full cycle of inhaling/exhaling is 4 seconds so...
    private static readonly ProtoId<MetabolismStagePrototype> RespirationStage = new("Respiration"); // no fucking idea what this is
    private TimeSpan _nextUpdate;
    private bool _spo2Enabled;

    private void InitializeInky()
    {
        _nextUpdate = _timing.CurTime + UpdateInterval;
        _cfg.OnValueChanged(InkyCVars.ComplexRespiratorEnabled, enabled => _spo2Enabled = enabled, true);
        SubscribeLocalEvent<AutismComponent, MapInitEvent>((ent, ref args) => UpdateBrainAlert(ent.Owner, brain => brain.AutismAlert, true));
        SubscribeLocalEvent<AutismComponent, ComponentShutdown>((ent, ref args) => UpdateBrainAlert(ent.Owner, brain => brain.AutismAlert, false));

        SubscribeLocalEvent<LobotomisedComponent, MapInitEvent>((ent, ref args) => UpdateBrainAlert(ent.Owner, brain => brain.LobotomyAlert, true));
        SubscribeLocalEvent<LobotomisedComponent, ComponentShutdown>((ent, ref args) => UpdateBrainAlert(ent.Owner, brain => brain.LobotomyAlert, false));

        SubscribeLocalEvent<BodyComponent, SaturateBrainEvent>(_body.RelayEvent);
        SubscribeLocalEvent<BrainComponent, BodyRelayedEvent<SaturateBrainEvent>>(OnSaturateBrain);
        SubscribeLocalEvent<BrainComponent, RejuvenateEvent>(OnRejuv);
    }

    private void OnRejuv(Entity<BrainComponent> ent, ref RejuvenateEvent ev)
    {
        ent.Comp.AirSaturation = 1f;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_spo2Enabled)
            return;

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        var eqe = EntityQueryEnumerator<BrainComponent>();
        while (eqe.MoveNext(out var uid, out var brain))
        {
            brain.AirSaturation = Math.Clamp(brain.AirSaturation - brain.AirConsumption, 0f, 1f);
            Dirty(uid, brain);

            var newLvl = GetOxygenLevel(brain.AirSaturation);
            if (newLvl == brain.OxygenLevel)
                continue;

            var oldLvl = brain.OxygenLevel;
            brain.OxygenLevel = newLvl;
            Dirty(uid, brain);

            var ev = new BrainOxygenLevelChangedEvent(uid, brain, oldLvl, newLvl);
            RaiseLocalEvent(uid, ref ev);
        }
    }

    private void OnSaturateBrain(Entity<BrainComponent> ent, ref BodyRelayedEvent<SaturateBrainEvent> args)
    {
        if (!TryComp<MetabolizerComponent>(args.Args.Lung, out var metabolizer)
            || !TryComp<LungComponent>(args.Args.Lung, out var lung)) // ok what the hell bro
            return;

        var saturation = GetSaturation(args.Args.Gas, (args.Args.Lung, metabolizer)) * lung.AirSaturationGain;
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
            var moths = gas[(int) gasId];
            if (moths <= 0f)
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

            var ammount = MathF.Min(moths * Atmospherics.BreathMolesToReagentMultiplier, 15f);

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

    private static BrainOxygen GetOxygenLevel(float saturation) // maybe its better to put it inside braincomp but whatever man
    {
        return saturation switch
        {
            > 0.9f => BrainOxygen.Stable,
            > 0.65f => BrainOxygen.Unstable,
            > 0.45f => BrainOxygen.Dangerous,
            > 0f => BrainOxygen.Critical,
            _ => BrainOxygen.Fatal,
        };
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
