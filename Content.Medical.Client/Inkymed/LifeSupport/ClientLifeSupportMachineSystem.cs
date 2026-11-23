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
            _appearance.SetData(uid, LifeSupportVisuals.ScreenState, state);
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

    /// <summary>
    /// Selects the monitor indicator using the first heart found in the connected patient's body.
    /// </summary>
    private LifeSupportScreenState GetVisualState(Entity<LifeSupportMachineComponent> ent, bool activated)
    {
        if (!activated)
            return LifeSupportScreenState.Off;

        if (ent.Comp.Connectee is not { } connectee)
            return LifeSupportScreenState.Indifferent;

        if (!TryComp<BodyComponent>(connectee, out var body))
            return LifeSupportScreenState.Dead;

        foreach (var heart in _body.GetOrgans<HeartComponent>((connectee, body)))
        {
            if (heart.Comp.CurrentRate <= 0)
                return LifeSupportScreenState.Dead;

            var (min, max) = heart.Comp.FibrillationCaps;
            return heart.Comp.CurrentRate < min || heart.Comp.CurrentRate > max
                ? LifeSupportScreenState.Critical
                : LifeSupportScreenState.Alive;
        }

        return LifeSupportScreenState.Dead;
    }
}
