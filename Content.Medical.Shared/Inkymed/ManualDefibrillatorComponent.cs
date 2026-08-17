using Content.Medical.Shared.Body;
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
    public Dictionary<int, float> BpmZapFlip = new()
    {
        { 0, 0f },
        { 1, -80f },
        { 2, -50f },
        { 3, 150f },
        { 4, 200f },
    };

    [DataField]
    public Dictionary<int, float> BpmZapFlatlineFlip = new()
    {
        { 0, 0f },
        { 1, 0f },
        { 2, 0f },
        { 3, 200f },
        { 4, 300f },
    };

    [DataField]
    public ProtoId<PulseStatePrototype> PulseState = "Pulse0";

    [ViewVariables]
    public Entity<HeartComponent>? HeartEntity;
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
