using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.Inkymed;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ManualDefibrillatorComponent : Component
{
    [DataField]
    public DefibrillatorChargeSetting ChargeSetting = DefibrillatorChargeSetting.None;

    [DataField]
    public ProtoId<PulseStatePrototype> PulseState = "Pulse0";

    [DataField]
    public EntityUid? TargetEntity;

    [DataField]
    public float Bpm;
}

[Flags]
public enum DefibrillatorChargeSetting : byte
{// todo inkymed guidebook, all of these are like 500v each todo
    None = 0,
    FirstFlip = 1 << 0,
    SecondFlip = 1 << 1,
    ThirdFlip = 1 << 2,
    FourthFlip = 1 << 3,

    PowerFlip = 1 << 4,

    AllFlips = FirstFlip | SecondFlip | ThirdFlip | FourthFlip | PowerFlip,
    AllMinusPower = FirstFlip | SecondFlip | ThirdFlip | FourthFlip
}
