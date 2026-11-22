// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Inkymed.LifeSupport;
using Content.Shared.Body;
using Content.Shared.DragDrop;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Medical.Server.Inkymed.LifeSupport;

public sealed partial class ServerLifeSupportMachineSystem : LifeSupportMachineSystem
{
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

    [SubscribeLocalEvent]
    private void OnCanDropDragged(Entity<LifeSupportMachineComponent> ent, ref CanDropDraggedEvent args)
    {
        if (args.Handled
            || !HasComp<BodyComponent>(args.Target))
            return;

        args.CanDrop = true;
        args.Handled = true;
    }
}
