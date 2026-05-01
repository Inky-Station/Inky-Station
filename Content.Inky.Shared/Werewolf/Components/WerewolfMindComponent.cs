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

    /// <summary>
    /// Transforms the werewolf automatically after the timer passes
    /// </summary>
    [DataField]
    public float TransfurmCycle = 90; // todo werewolf 600

    /// <summary>
    /// After what time should the warning popup appear
    /// </summary>
    [DataField]
    public float TransfurmWarnDelay = 60f;

    /// <summary>
    /// After what amount of time can the entity transfurm on command again
    /// </summary>
    [DataField]
    public float TransfurmOnCommandDelay = 30f;

    /// <summary>
    /// Can you transfurm right now
    /// </summary>
    [DataField]
    public bool TransfurmReady;

    [DataField]
    public bool HasWarned; // to not spam shit

    [ViewVariables]
    public LocId TransfurmPopup = "werewolf-transfurm-warn";

    [ViewVariables]
    public LocId TransfurmReadyPopup = "werewolf-transfurm-ready";

    [ViewVariables]
    public float Accumulator = 0f;
}
