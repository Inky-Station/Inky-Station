// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Medical.Shared.Inkymed.LifeSupport;

/// <summary>
/// Stores the life support machine's connection settings, its networked patient reference,
/// and the organ values saved by the server while support is active.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LifeSupportMachineComponent : Component
{
    [DataField]
    public SpriteSpecifier RopeSprite = new SpriteSpecifier.Rsi(new ResPath("_RMC14/Objects/phone/phone.rsi"), "rope"); // trol

    [DataField]
    public float JointMaxLength = 1f;

    [DataField]
    public float JointStiffness = 0.5f;

    [DataField]
    public float HeartRateUpdateMultiplier = 0.5f;

    [DataField, AutoNetworkedField]
    public EntityUid? Connectee;

    /// <summary>
    /// Heart rate update modifiers saved before applying life support, keyed by heart entity.
    /// </summary>
    public readonly Dictionary<EntityUid, float> OriginalHeartModifiers = new();

    /// <summary>
    /// Brain air consumption values saved before applying life support, keyed by brain entity.
    /// </summary>
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
