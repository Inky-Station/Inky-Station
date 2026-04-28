namespace Content.Inky.Shared.Werewolf.Components;

// fucking KILL YOURSELF!!!!
[RegisterComponent]
public sealed partial class WerewolfMindComponent : Component
{
    [DataField]
    public List<EntityUid> BittenPeople = new(); // would be used in the manifest TODO WEREWOLF

    [DataField]
    public List<string> UnlockedActions = new();

    [DataField]
    public int Currency; // needed becasue polymorph & store shitcode
}
