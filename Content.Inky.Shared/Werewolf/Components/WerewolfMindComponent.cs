using Content.Shared.Polymorph;
using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Inky.Shared.Werewolf.Components;

// fucking KILL YOURSELF!!!!
[RegisterComponent]
public sealed partial class WerewolfMindComponent : Component // todo werewolf debloat?
{
    [DataField]
    public List<EntityUid> BittenPeople = []; // would be used in the manifest TODO WEREWOLF

    /// <summary>
    /// Used by the black wolf to show which entities were turned into werewolves by him.
    /// Stores MIND ent uids, not body uids, bodies change on polymorph, minds dont.
    /// </summary>
    [DataField]
    public List<EntityUid> PackMembers = [];

    /// <summary>
    /// The ent currently being hunted by this werewolf
    /// </summary>
    [DataField]
    public EntityUid? CurrentMarkedVictim;

    /// <summary>
    /// If true, this werewolf wouldnt be counted for marking by other wolves
    /// </summary>
    [DataField]
    public bool MarkImmune; // also holy shit this is starting to look like a bloated comp

    [DataField]
    public int Currency; // needed becasue polymorph & store shitcode

    [DataField]
    public ProtoId<PolymorphPrototype>? CurrentMutation;

    [DataField]
    public HashSet<ProtoId<StoreCategoryPrototype>> StoreCategories = [];
    #region transform

    /// <summary>
    /// Transforms the werewolf automatically after the timer passes
    /// </summary>
    [DataField]
    public TimeSpan TransfurmCycle = TimeSpan.FromSeconds(600);

    /// <summary>
    /// After what time should the warning popup appear
    /// </summary>
    [DataField]
    public TimeSpan TransfurmWarnDelay = TimeSpan.FromSeconds(540);

    /// <summary>
    /// After what amount of time can the entity transfurm on command again
    /// </summary>
    [DataField]
    public TimeSpan TransfurmOnCommandDelay = TimeSpan.FromSeconds(120);

    /// <summary>
    /// Can you transfurm right now
    /// </summary>
    [DataField]
    public bool TransfurmReady;

    [DataField]
    public bool BlockTransfurm;

    [DataField]
    public bool HasWarned; // to not spam shit

    [ViewVariables]
    public LocId TransfurmPopup = "werewolf-transfurm-warn";

    [ViewVariables]
    public LocId TransfurmReadyPopup = "werewolf-transfurm-ready";

    [ViewVariables]
    public TimeSpan Accumulator = TimeSpan.Zero;

    [ViewVariables] // supriisngly used for marked guys
    public TimeSpan AccumulatorPopup = TimeSpan.Zero;
    #endregion
}
