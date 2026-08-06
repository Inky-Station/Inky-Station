using Content.Shared.Alert;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Components;

public sealed partial class BrainComponent
{
    [DataField, AutoNetworkedField]
    public float AirSaturation = 1f;

    [DataField, AutoNetworkedField]
    public float AirConsumption = 0.8f;

    [DataField, AutoNetworkedField]
    public float AirSaturationGain = 0.05f;

    [DataField]
    public ProtoId<AlertPrototype>? AutismAlert = "Autism";

    [DataField]
    public ProtoId<AlertPrototype>? LobotomyAlert = "Lobotomy";

}
