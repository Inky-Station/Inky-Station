using Content.Shared.Alert;

namespace Content.Medical.Shared.Body;

public sealed partial class HeartComponent
{
    /// <summary>
    /// The starting heartrate AKA what it should be
    /// </summary>
    [DataField]
    public int StartingHeartRate = 100;

    [DataField]
    public int MaxHeartRate = 300;

    [DataField]
    public int MinHeartRate = 0;

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
    public int FibrillationCap = 50;

    [ViewVariables, AutoNetworkedField]
    public int CurrentHeartRate;

    /// <summary>
    /// Set to true if its in a body that has MobState and its not MobState.Dead
    /// to prevent updating dead hearts
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool CurrentlyActive { get; set; }

    /// <summary>
    /// If true, will stop stabilisation
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool CurrentlyFibrillating = false;

    [DataField]
    public ProtoId<AlertPrototype>? FibrillationAlert = "Fibrillations";

    [DataField]
    public ProtoId<AlertPrototype>? HeartStopAlert = "HeartStop";
}
