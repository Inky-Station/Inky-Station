using Robust.Shared.Prototypes;

namespace Content.Inky.Shared.Misc;

[RegisterComponent]
public sealed partial class FlipDoStatusEffectComponent : Component
{
    [DataField]
    public EntProtoId StatusEffect = "StatusEffectProjectileImmunity";

    [DataField]
    public float StaminaCost = 15f;
}
