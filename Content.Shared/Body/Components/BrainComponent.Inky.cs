using Content.Shared.Alert;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Components;

public sealed partial class BrainComponent
{
    [DataField, AutoNetworkedField]
    public float AirSaturation = 1f;

    [DataField, AutoNetworkedField]
    public float AirConsumption = 0.04f;

    [ViewVariables, AutoNetworkedField]
    public BrainOxygen OxygenLevel = BrainOxygen.Stable;

    [DataField]
    public ProtoId<AlertPrototype>? AutismAlert = "Autism";

    [DataField]
    public ProtoId<AlertPrototype>? LobotomyAlert = "Lobotomy";

}

[Serializable, NetSerializable]
public enum BrainOxygen : byte
{
    Stable,
    Unstable,
    Dangerous,
    Critical,
    Fatal,
}

[ByRefEvent]
public readonly record struct BrainOxygenLevelChangedEvent(
    EntityUid Target,
    BrainComponent Component,
    BrainOxygen OldLevel,
    BrainOxygen NewLevel);
