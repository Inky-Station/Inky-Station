using Content.Shared.Body;
using Content.Shared.DragDrop;
using Content.Shared.Physics;

namespace Content.Medical.Shared.Inkymed.LifeSupport;

/// <summary>
/// This handles...
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

    protected virtual void OnConnecteeChanged(Entity<LifeSupportMachineComponent> ent, EntityUid? previous) { }
}
