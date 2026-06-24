using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.Inkymed;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ManualDefibrillatorComponent : Component
{
    [DataField]
    public DefibrillatorChargeSetting ChargeSetting = new();

    [DataField]
    public int[] BpmZapFlip = [0, -80, -50, 150, 200];

    [DataField]
    public int[] BpmZapFlatlineFlip = [0, 0, 0, 200, 300];

    [DataField]
    public ProtoId<PulseStatePrototype> PulseState = "Pulse0";

    [DataField]
    public EntityUid? TargetEntity;

    [DataField]
    public float Bpm;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class DefibrillatorChargeSetting
{
    public const int FlipAmount = 4;

    [DataField]
    public bool Power;

    [DataField]
    public bool[] Flips = new bool[FlipAmount];

    public DefibrillatorChargeSetting Clone()
    {
        return new DefibrillatorChargeSetting
        {
            Power = Power,
            Flips = (bool[])Flips.Clone(),
        };
    }
}
