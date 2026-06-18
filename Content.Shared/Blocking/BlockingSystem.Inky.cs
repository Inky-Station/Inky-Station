using Content.Shared.Movement.Systems;

namespace Content.Shared.Blocking;

public sealed partial class BlockingSystem
{
    public void InitializeInky()
    {
        SubscribeLocalEvent<BlockingUserComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
    }

    public void OnRefreshMovespeed(EntityUid uid, BlockingUserComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        if (!_blockQuery.TryComp(comp.BlockingItem, out var blockingComp)
            || !blockingComp.IsBlocking)
            return;

        args.ModifySpeed(comp.MovementModifier, comp.MovementModifier);
    }
}
