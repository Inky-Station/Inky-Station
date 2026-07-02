using Content.Shared.Alert;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Components;

public sealed partial class BrainComponent
{
    [DataField]
    public ProtoId<AlertPrototype>? AutismAlert = "Autism";

    [DataField]
    public ProtoId<AlertPrototype>? LobotomyAlert = "Lobotomy";

}
