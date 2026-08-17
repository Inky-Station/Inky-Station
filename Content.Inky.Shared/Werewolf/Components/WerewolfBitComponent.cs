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

    /// <summary>
    /// After what time should the entity become a werewolf if bitten
    /// </summary>
    [DataField]
    public TimeSpan LycTimer = TimeSpan.FromSeconds(30); // todo 600

    [ViewVariables]
    public TimeSpan Accumulator = TimeSpan.Zero;
}
