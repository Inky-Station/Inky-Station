using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Inky.Server.Goidarmory;

[RegisterComponent]
public sealed partial class GoidarmoryComponent : Component
{
    [DataField(required: true)]
    public ProtoId<JobPrototype> Role;

    [DataField(required: true)]
    public EntProtoId Item;

    [DataField]
    public float CountPerRole = 1;
}
