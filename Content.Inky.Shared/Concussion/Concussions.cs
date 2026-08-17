using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Robust.Shared.Serialization;

namespace Content.Inky.Shared.Concussion;

/// <summary>
/// defines the concussion state of an entity
/// ordered from most sane to "oh shiiii"
/// </summary>
[Serializable, NetSerializable]
public enum ConcussionState : byte // todo maybe rename the values to not be dogshit
{
    Invalid = 0,
    Sane = 1,
    Minor = 2,
    Hard = 3,
    Overwhelmed = 4 // currently isnt used anywhere but is here for idk super brain damage?
}

/// <summary>
/// Event that is raised whenever <see cref="ConcussionState"/> changes on an entity
/// </summary>
/// <param name="Target">The entity that has the concussion state changed</param>
/// <param name="Component"><see cref="OldState"/> component</param>
/// <param name="OldState">Previous concussion state</param>
/// <param name="NewState">New concussion state</param>
[ByRefEvent]
public readonly record struct ConcussionStateChangedEvent(EntityUid Target, ConcussionThresholdComponent Component,
    ConcussionState OldState, ConcussionState NewState);

/// <summary>
/// Raises before Target recieves concussion damage
/// </summary>
/// <param name="Target">Targetted EntityUid that will recieve the damage</param>
/// <param name="Component"><see cref="ConcussionThresholdComponent"/></param>
/// <param name="Damage">Damage that X has dealt to the target</param>
/// <param name="Cancelled">Is cancelled?</param>
[ByRefEvent]
public record struct BeforeConcussionDamageEvent(EntityUid Target, ConcussionThresholdComponent Component, FixedPoint2 Damage, bool Cancelled = false) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.All;
}
