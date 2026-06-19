using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.Timing;

namespace Content.Inky.Shared.Concussion;

public abstract class SharedConcussionSystem : EntitySystem
{
    private const float _absoluteCap = 200f; // imagine maxcapping yourself, having 4k concussion damage and being revived at med forced to sit for 11 hours to heal

    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    /// <summary>
    /// updates <see cref="ConcussionThresholdComponent.CurrentState"/> based on
    /// <see cref="ConcussionThresholdComponent.StoredDamage"/> and raises
    /// <see cref="ConcussionStateChangedEvent"/> if the state changed
    /// </summary>
    protected void UpdateConcussionState(EntityUid uid, ConcussionThresholdComponent comp)
    {
        var newState = GetStateForDamage(comp);
        if (newState == comp.CurrentState)
            return;

        var oldState = comp.CurrentState;
        comp.CurrentState = newState;

        var ev = new ConcussionStateChangedEvent(uid, comp, oldState, newState);
        RaiseLocalEvent(uid, ref ev);
    }

    private ConcussionState GetStateForDamage(ConcussionThresholdComponent comp)
    {
        var result = ConcussionState.Sane;

        foreach (var (threshold, state) in comp.Thresholds)
        {
            if (comp.StoredDamage < threshold)
                break;

            if (comp.AllowedStates.Contains(state))
                result = state;
        }

        return result;
    }

    public void AddConcussionDamage(EntityUid uid, ConcussionThresholdComponent comp, FixedPoint2 damage)
    {
        var ev = new BeforeConcussionDamageEvent(uid, comp, damage);
        if (TryComp<InventoryComponent>(uid, out var inventoryComp))
            _inventory.RelayEvent((uid, inventoryComp), ref ev);

        if (ev.Cancelled)
            return;

        var cap = (FixedPoint2)_absoluteCap; // fuck you
        foreach (var (threshold, state) in comp.Thresholds)
        {
            if (state == ConcussionState.Overwhelmed)
            {
                cap = threshold;
                break;
            }
        }

        comp.StoredDamage = FixedPoint2.Min(comp.StoredDamage + ev.Damage, cap);
        UpdateConcussionState(uid, comp);
        Dirty(uid, comp);
        comp.NextUpdate = _timing.CurTime + comp.UpdateInterval;
    }
}
