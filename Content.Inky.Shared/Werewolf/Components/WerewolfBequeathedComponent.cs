using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Inky.Shared.Werewolf.Components;

/// <summary>
/// On <see cref="OriginalLeader"/> death, entity with this component will be force-mutated into a black wolf.
/// </summary>
[RegisterComponent]
public sealed partial class WerewolfBequeathedComponent : Component
{
    [DataField] public WerewolfMindComponent? OriginalLeader;
    public readonly ProtoId<StoreCategoryPrototype> Store = new("WerewolfBlack"); // goida
}
