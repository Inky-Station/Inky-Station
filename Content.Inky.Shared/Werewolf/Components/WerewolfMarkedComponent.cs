namespace Content.Inky.Shared.Werewolf.Components;

/// <summary>
/// Marks a werewolf that it is being hunted by another
/// </summary>
[RegisterComponent]
public sealed partial class WerewolfMarkedComponent : Component
{
    [DataField]
    public EntityUid MarkedBy;
}
