using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Inky.Shared.Concussion;

/// <summary>
/// Entities with this component will have concussion states & could be concussed.
/// To properly work, the entity should have <see cref="DamageableComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class ConcussionThresholdComponent : Component
{
    [AutoNetworkedField, ViewVariables]
    public ConcussionState CurrentState { get; set; } = ConcussionState.Sane;

    [DataField("thresholds", required: true)]
    public SortedDictionary<FixedPoint2, ConcussionState> Thresholds = new();

    [DataField]
    [AutoNetworkedField]
    public HashSet<ConcussionState> AllowedStates = new()
    {
        ConcussionState.Sane,
        ConcussionState.Minor,
        ConcussionState.Hard,
        ConcussionState.Overwhelmed
    };

    /// <summary>
    /// links concussion state to a movement speed modifier (0-1, 1 is full speed)
    /// </summary>
    [DataField]
    public Dictionary<ConcussionState, float> SpeedModifierThresholds = new();

    [AutoNetworkedField, ViewVariables]
    public FixedPoint2 StoredDamage { get; set; } = FixedPoint2.Zero;

    /// <summary>
    /// How much damage heals per second
    /// </summary>
    [DataField]
    public float HealRate = 1f; // this is horrible shit but i just dont wanna make another damage group specifically for ts

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [ViewVariables]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.5);
}

