using Content.Shared.Alert;

namespace Content.Medical.Shared.Body;

public sealed partial class HeartComponent
{
    /// <summary>
    /// The starting heartrate AKA what it should be
    /// </summary>
    [DataField]
    public float NormalHeartRate = 100f;

    [DataField]
    public float MaxHeartRate = 300f;

    [DataField]
    public float MinHeartRate = 0f;

    /// <summary>
    /// amount of heartrate being added or reduced per second
    /// aims to be at StartingHeartRate
    /// </summary>
    [DataField]
    public float StabilisationRate = 1f;

    /// <summary>
    /// if the current heartrate is +FibrillationCap or -FibrillationCap from the starting heart rate,
    /// the entity will receive a fibrillation alert and will stop stabilising on itself,
    /// eventually reaching either min or max cap on the heartrate
    /// </summary>
    [DataField]
    public float FibrillationCap = 50f;

    [ViewVariables, AutoNetworkedField]
    public float CurrentHeartRate;

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
