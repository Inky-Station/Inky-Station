namespace Content.Inky.Shared.Werewolf.Components;

/// <summary>
/// Marks the person as bitten by a werewolf
/// this is given when an entity is a target for the werewolfdevour & other path specific bitings
/// </summary>
[RegisterComponent]
public sealed partial class WerewolfBitComponent : Component // todo loc strings for popups?
{
    [DataField] public WerewolfMindComponent? BittenBy;

    /// <summary>
    /// If the entity is in the proccess of turning into a werewolf
    /// </summary>
    [DataField]
    public bool Infected;

    [ViewVariables]
    public float Accumulator = 0f;

    /// <summary>
    /// After what time should the entity become a werewolf if bitten
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float LycTimer = 30f; // todo 600
}
