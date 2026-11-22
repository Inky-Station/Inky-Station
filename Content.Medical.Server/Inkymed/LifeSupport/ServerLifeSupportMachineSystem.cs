// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Body;
using Content.Medical.Shared.Inkymed.LifeSupport;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Medical.Server.Inkymed.LifeSupport;

public sealed partial class ServerLifeSupportMachineSystem : LifeSupportMachineSystem
{
    private const string LifeSupportJoint = "life-support";

    [Dependency] private SharedJointSystem _joints = default!;
    [Dependency] private BodySystem _body = default!;

    [SubscribeLocalEvent]
    private void OnToggled(Entity<LifeSupportMachineComponent> ent, ref ItemToggledEvent args)
    {
        if (args.Activated && ent.Comp.Connectee is { } patient)
            ApplyModif(ent, patient);
        else if (!args.Activated)
            ResetModifs(ent);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<LifeSupportMachineComponent> ent, ref ComponentShutdown args)
    {
        ResetModifs(ent);
    }

    protected override void OnConnecteeChanged(Entity<LifeSupportMachineComponent> ent, EntityUid? previous)
    {
        if (previous != null)
        {
            _joints.RemoveJoint(ent.Owner, LifeSupportJoint);
            ResetModifs(ent);
        }

        if (ent.Comp.Connectee is not { } patient)
            return;

        var jointComp = EnsureComp<JointComponent>(ent.Owner);
        var joint = _joints.CreateDistanceJoint(ent.Owner, patient, id: LifeSupportJoint);
        joint.MaxLength = 1f;
        joint.MinLength = 0f;
        joint.Stiffness = 0.5f;
        Dirty(ent.Owner, jointComp);

        if (TryComp<ItemToggleComponent>(ent.Owner, out var toggle) && toggle.Activated)
            ApplyModif(ent, patient);
    }

    private void ApplyModif(Entity<LifeSupportMachineComponent> ent, EntityUid patient)
    {
        if (!TryComp<BodyComponent>(patient, out var body))
            return;

        foreach (var heart in _body.GetOrgans<HeartComponent>((patient, body)))
        {
            ent.Comp.OriginalHeartModifiers[heart.Owner] = heart.Comp.RateUpdateModifier;
            heart.Comp.RateUpdateModifier /= 2f;
            Dirty(heart);
        }

        foreach (var brain in _body.GetOrgans<BrainComponent>((patient, body)))
        {
            ent.Comp.OriginalBrainModifier[brain.Owner] = brain.Comp.AirConsumption;
            var ev = new SetBrainAirConsumptionEvent(-MathF.Abs(brain.Comp.AirConsumption));
            RaiseLocalEvent(brain.Owner, ref ev);
        }
    }

    private void ResetModifs(Entity<LifeSupportMachineComponent> ent)
    {
        foreach (var (heartUid, rateUpdateModifier) in ent.Comp.OriginalHeartModifiers)
        {
            if (!TryComp<HeartComponent>(heartUid, out var heart))
                continue;

            heart.RateUpdateModifier = rateUpdateModifier;
            Dirty(heartUid, heart);
        }

        foreach (var (brainUid, airConsumption) in ent.Comp.OriginalBrainModifier)
        {
            if (!HasComp<BrainComponent>(brainUid))
                continue;

            var ev = new SetBrainAirConsumptionEvent(airConsumption);
            RaiseLocalEvent(brainUid, ref ev);
        }

        ent.Comp.OriginalHeartModifiers.Clear();
        ent.Comp.OriginalBrainModifier.Clear();
    }
}
