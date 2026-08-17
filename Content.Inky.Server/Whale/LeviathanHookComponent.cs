namespace Content.Inky.Server.Whale;

[RegisterComponent]
public sealed partial class LeviathanHookComponent : Component
{
    [DataField] public EntityUid Leviathan;
    [DataField] public EntityUid Pilot;
}
