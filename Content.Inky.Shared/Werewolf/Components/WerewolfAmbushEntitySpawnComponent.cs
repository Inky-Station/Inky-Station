using Robust.Shared.Prototypes;

namespace Content.Inky.Shared.Werewolf.Components;

/// <summary>
/// Temporarily causes an ambushing werewolf to leave an entity behind on each tile it enters.
/// </summary>
[RegisterComponent]
public sealed partial class WerewolfAmbushEntitySpawnComponent : Component
{
    public EntProtoId Prototype = default!;
    public TimeSpan EndTime;
}
