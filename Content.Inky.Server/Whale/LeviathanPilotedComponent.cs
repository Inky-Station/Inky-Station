using Content.Server.NPC.HTN;
using Robust.Shared.Prototypes;

namespace Content.Inky.Server.Whale;

[RegisterComponent]
public sealed partial class LeviathanPilotedComponent : Component
{
    [DataField] public EntityUid Pilot;
    [DataField] public EntityUid Hook;

    [DataField]
    public ProtoId<HTNCompoundPrototype>? StoredHTNTask;
}

[RegisterComponent]
public sealed partial class LeviathanPilotComponent : Component
{
    [DataField] public EntityUid Leviathan;
}

[RegisterComponent]
public sealed partial class LeviathanHookComponent : Component
{
    [DataField] public EntityUid Leviathan;
    [DataField] public EntityUid Pilot;
}
