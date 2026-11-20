using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Inky.Shared.Werewolf.Components;

/// <summary>
/// spawns an entity under the entity with the component on move
/// </summary>
// also might actually not work as intended because uhmmm godia
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SpawnEntitiesOnStepComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Prototype;

    [DataField]
    public float MinimumVelocitySquared = 0.25f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextSpawnTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan SpawnPeriod = TimeSpan.FromSeconds(0.05f); // lol
}
