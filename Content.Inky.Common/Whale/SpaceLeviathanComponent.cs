using Robust.Shared.Audio;

namespace Content.Inky.Common.Whale;

[RegisterComponent]
public sealed partial class SpaceLeviathanComponent : Component
{
    [ViewVariables]
    public List<EntityUid?> Targets = new List<EntityUid?>();
}
