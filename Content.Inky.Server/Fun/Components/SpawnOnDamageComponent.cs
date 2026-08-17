using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Inky.Server.Fun.Components;

[RegisterComponent]
public sealed partial class SpawnOnDamageComponent : Component
{
    [DataField]
    public EntProtoId Spawn = "WeaponTurretFunEngi";

    [DataField]
    public bool Active;

    // required ammount of items in a stack
    [DataField]
    public int StackRequired = 30;

    [DataField]
    public bool EngiOnly = true; // will spawn the entity only if hit by engineering staff
}
