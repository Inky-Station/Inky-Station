using Content.Shared.Alert;
using Content.Shared.FixedPoint;

namespace Content.Medical.Shared.Body;

public sealed partial class HeartComponent
{
    /// <summary>
    /// The starting heartrate AKA what it should be
    /// </summary>
    [DataField]
    public float NormalRate = 80f;

    /// <summary>
    /// After what BPM the heartrate becomes critical
    /// </summary>
    [DataField]
    public float CriticalRate = 290f;

    /// <summary>
    /// After MaxHeartRate is reached, every second the heart has an X% chance of stopping
    /// </summary>
    [DataField]
    public float CriticalStopChance = 0.03f;

    /// <summary>
    /// extra factor for the parabolic formula
    /// cbrt((cur - norm)(cur - minfib)(cur - maxfib))
    /// </summary>
    [DataField]
    public float RateUpdateModifier = 0.02f;

    /// <summary>
    /// if the current heartrate is beyond fibrillation caps,
    /// the entity will receive a fibrillation alert and will stop stabilising on itself,
    /// eventually reaching into a cap of the heartrate
    /// </summary>
    [DataField]
    public Vector2 FibrillationCaps = new(40f, 210f);

    [ViewVariables, AutoNetworkedField]
    public float CurrentRate;

    [DataField]
    public ProtoId<AlertPrototype>? FibrillationAlert = "Fibrillations";

    [DataField]
    public ProtoId<AlertPrototype>? HeartStopAlert = "HeartStop";
}

/// raised on the body when its heart state changes
[ByRefEvent]
public struct HeartStateChangedEvent(HeartState oldState, HeartState newState)
{
    public readonly HeartState OldState = oldState;
    public readonly HeartState NewState = newState;
}

public enum HeartState
{
    Stable,
    Fibrillating,
    Stopped,
}
