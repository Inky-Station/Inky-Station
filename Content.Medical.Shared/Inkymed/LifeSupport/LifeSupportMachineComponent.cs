namespace Content.Medical.Shared.Inkymed.LifeSupport;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LifeSupportMachineComponent : Component
{
    [DataField]
    public SpriteSpecifier RopeSprite = new SpriteSpecifier.Rsi(new ResPath("_RMC14/Objects/phone/phone.rsi"), "rope"); // trol

    [DataField, AutoNetworkedField]
    public EntityUid? Connectee;

    public readonly Dictionary<EntityUid, float> OriginalHeartModifiers = new();
    public readonly Dictionary<EntityUid, float> OriginalBrainModifier = new();
}

[Serializable, NetSerializable]
public enum LifeSupportVisuals : byte
{
    Indifferent,
    Dead,
    Alive,
    Critical,
}
