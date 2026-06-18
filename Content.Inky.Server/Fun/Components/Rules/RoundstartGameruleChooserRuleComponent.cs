using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Inky.Server.Fun.Components.Rules;

/// <summary>
/// Adds every gamerule defined in its Rules when ran
/// FunOnly adds each Rule with a % chance defined in the InkyCVars.FunProb
/// </summary>
[RegisterComponent]
public sealed partial class RoundstartGameruleChooserRuleComponent : Component
{
    [DataField]
    public List<string> Rules = new();

    [DataField]
    public bool FunOnly;
}
