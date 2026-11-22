using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.DragDrop;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Physics;
using Content.Medical.Shared.Body;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Medical.Shared.Inkymed.LifeSupport;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class LifeSupportMachineSystem : EntitySystem
{
    private const string LifeSupportJoint = "life-support";

    [Dependency] private SharedJointSystem _joints = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private BrainSystem _brain = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<LifeSupportMachineComponent> ent, ref MapInitEvent args)
        => UpdateVisuals(ent);

    [SubscribeLocalEvent]
    private void OnToggled(Entity<LifeSupportMachineComponent> ent, ref ItemToggledEvent args)
    {
        if (args.Activated && ent.Comp.Connectee is { } patient)
            ApplyModif(ent, patient);
        else if (!args.Activated)
            ResetModifs(ent);

        UpdateVisuals(ent, args.Activated);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<LifeSupportMachineComponent> ent, ref ComponentShutdown args)
    {
        ResetModifs(ent);
    }

    public override void Update(float frameTime)
    {
        var eqe = EntityQueryEnumerator<LifeSupportMachineComponent, ItemToggleComponent>();
        while (eqe.MoveNext(out var uid, out var lifeSupport, out var toggle))
        {
            var ent = (uid, lifeSupport);
            UpdateVisuals(ent, toggle.Activated);
        }
    }

    [SubscribeLocalEvent]
    private void OnCanDropDragged(Entity<LifeSupportMachineComponent> ent, ref CanDropDraggedEvent args)
    {
        if (args.Handled
            || !HasComp<BodyComponent>(args.Target))
            return;

        args.CanDrop = true;
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnCanDrag(Entity<LifeSupportMachineComponent> ent, ref CanDragEvent args) // why is it so shit  bro
        => args.Handled = true;

    [SubscribeLocalEvent]
    private void OnDragDropDragged(Entity<LifeSupportMachineComponent> ent, ref DragDropDraggedEvent args)
    {
        if (args.Handled
            || !HasComp<BodyComponent>(args.Target))
            return;

        if (ent.Comp.Connectee is { } connectee) // disconnect if dragged a second time
        {
            _joints.RemoveJoint(ent.Owner, LifeSupportJoint);
            RemComp<JointVisualsComponent>(ent.Owner);
            ResetModifs(ent);
            ent.Comp.Connectee = null;
            Dirty(ent);
            UpdateVisuals(ent);

            if (connectee == args.Target)
            {
                args.Handled = true;
                return;
            }
        }

        if (ent.Comp.Connectee is not null)
            return;

        var visual = EnsureComp<JointVisualsComponent>(ent.Owner);
        visual.Sprite = ent.Comp.RopeSprite;
        visual.Target = args.Target;
        Dirty(ent.Owner, visual);

        var jointcomp = EnsureComp<JointComponent>(ent.Owner);
        var joint = _joints.CreateDistanceJoint(ent.Owner, args.Target, id: LifeSupportJoint);
        joint.MaxLength = 1f; // todo put inside a comp ?
        joint.MinLength = 0f;
        joint.Stiffness = 0.5f;
        Dirty(ent.Owner, jointcomp);

        ent.Comp.Connectee = args.Target;
        if (TryComp<ItemToggleComponent>(ent.Owner, out var toggle) && toggle.Activated)
            ApplyModif(ent, args.Target);

        Dirty(ent);
        UpdateVisuals(ent);

        args.Handled = true;
    }

    private void UpdateVisuals(Entity<LifeSupportMachineComponent> ent, bool? activated = null)
    {
        activated ??= TryComp<ItemToggleComponent>(ent.Owner, out var toggle) && toggle.Activated;
        var state = GetVisualState(ent, activated.Value);

        _appearance.SetData(ent, LifeSupportVisuals.Indifferent, state == LifeSupportVisuals.Indifferent);
        _appearance.SetData(ent, LifeSupportVisuals.Alive, state == LifeSupportVisuals.Alive); // WHATEVER MAN IT WORKS
        _appearance.SetData(ent, LifeSupportVisuals.Critical, state == LifeSupportVisuals.Critical);
        _appearance.SetData(ent, LifeSupportVisuals.Dead, state == LifeSupportVisuals.Dead);
    }

    private LifeSupportVisuals? GetVisualState(Entity<LifeSupportMachineComponent> ent, bool activated)
    {
        if (!activated)
            return null;

        if (ent.Comp.Connectee is not { } connectee)
            return LifeSupportVisuals.Indifferent;

        if (!TryComp<BodyComponent>(connectee, out var body))
            return LifeSupportVisuals.Dead;

        foreach (var heart in _body.GetOrgans<HeartComponent>((connectee, body)))
        {
            if (heart.Comp.CurrentRate <= 0)
                return LifeSupportVisuals.Dead;

            var (min, max) = heart.Comp.FibrillationCaps;
            return heart.Comp.CurrentRate < min || heart.Comp.CurrentRate > max
                ? LifeSupportVisuals.Critical
                : LifeSupportVisuals.Alive;
        }

        return LifeSupportVisuals.Dead;
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
            _brain.SetAirConsumption(brain.AsNullable(), -MathF.Abs(brain.Comp.AirConsumption));
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
            if (!TryComp<BrainComponent>(brainUid, out var brain))
                continue;

            _brain.SetAirConsumption((brainUid, brain), airConsumption);
        }

        ent.Comp.OriginalHeartModifiers.Clear();
        ent.Comp.OriginalBrainModifier.Clear();
    }
}
