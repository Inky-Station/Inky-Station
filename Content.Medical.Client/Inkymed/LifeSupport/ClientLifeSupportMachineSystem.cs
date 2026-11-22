// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Body;
using Content.Medical.Shared.Inkymed.LifeSupport;
using Content.Shared.Body;
using Content.Shared.DragDrop;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Medical.Client.Inkymed.LifeSupport;

public sealed partial class ClientLifeSupportMachineSystem : LifeSupportMachineSystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private BodySystem _body = default!;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var eqe = EntityQueryEnumerator<LifeSupportMachineComponent, ItemToggleComponent>();
        while (eqe.MoveNext(out var uid, out var lifeSupport, out var toggle))
        {
            var state = GetVisualState((uid, lifeSupport), toggle.Activated);

            _appearance.SetData(uid, LifeSupportVisuals.Indifferent, state == LifeSupportVisuals.Indifferent);
            _appearance.SetData(uid, LifeSupportVisuals.Alive, state == LifeSupportVisuals.Alive); // WHATEVER MAN IT WORKS
            _appearance.SetData(uid, LifeSupportVisuals.Critical, state == LifeSupportVisuals.Critical);
            _appearance.SetData(uid, LifeSupportVisuals.Dead, state == LifeSupportVisuals.Dead);
        }
    }

    [SubscribeLocalEvent]
    private void OnCanDrag(Entity<LifeSupportMachineComponent> ent, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnCanDropDragged(Entity<LifeSupportMachineComponent> ent, ref CanDropDraggedEvent args)
    {
        if (args.Handled || !HasComp<BodyComponent>(args.Target))
            return;

        args.CanDrop = true;
        args.Handled = true;
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
}
