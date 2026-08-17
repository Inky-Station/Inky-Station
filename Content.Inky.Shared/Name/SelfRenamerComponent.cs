using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Inky.Shared.Name;

[RegisterComponent]
public sealed partial class SelfRenamerComponent : Component
{
    [DataField]
    public EntProtoId ActionPrototype = "ActionSelfRename";

    [DataField]
    public EntityUid? ActionEntity;
}

[NetSerializable, Serializable]
public enum SelfRenamerUIKey : byte
{
    Key
}
