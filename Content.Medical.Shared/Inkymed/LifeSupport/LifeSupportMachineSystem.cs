// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.DragDrop;
using Content.Shared.Physics;

namespace Content.Medical.Shared.Inkymed.LifeSupport;

/// <summary>
/// Connects and disconnects patients through drag and drop and updates the cable visuals.
/// </summary>
public abstract partial class LifeSupportMachineSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnDragDropDragged(Entity<LifeSupportMachineComponent> ent, ref DragDropDraggedEvent args)
    {
        if (args.Handled
            || !HasComp<BodyComponent>(args.Target))
            return;

        var previous = ent.Comp.Connectee;
        ent.Comp.Connectee = previous == args.Target ? null : args.Target;

        if (ent.Comp.Connectee is { } patient)
        {
            var visual = EnsureComp<JointVisualsComponent>(ent.Owner);
            visual.Sprite = ent.Comp.RopeSprite;
            visual.Target = patient;
            Dirty(ent.Owner, visual);
        }
        else
        {
            RemComp<JointVisualsComponent>(ent.Owner);
        }

        Dirty(ent);
        args.Handled = true;
        OnConnecteeChanged(ent, previous);
    }

    /// <summary>
    /// Runs after drag and drop updates <see cref="LifeSupportMachineComponent.Connectee"/> and the cable visuals.
    /// The new patient is available on the component, with <see langword="null"/> indicating a disconnection.
    /// </summary>
    /// <param name="ent">The life support machine whose connection changed.</param>
    /// <param name="previous">The previously connected patient, or <see langword="null"/> if none was connected.</param>
    protected virtual void OnConnecteeChanged(Entity<LifeSupportMachineComponent> ent, EntityUid? previous) { }
}
