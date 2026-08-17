using Content.Shared.Polymorph;
using Content.Trauma.Common.CollectiveMind;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Inky.Shared.Werewolf.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WerewolfAbilitiesComponent : Component
{
    [DataField] public SoundSpecifier ShriekSound = new SoundPathSpecifier("/Audio/_Inky/Antag/Werewolf/howl.ogg");
    [DataField] public SoundSpecifier DistantSound = new SoundPathSpecifier("/Audio/_Inky/Antag/Werewolf/howl.ogg"); // todo werewolf
    [DataField] public SoundSpecifier RipSound = new SoundPathSpecifier("/Audio/Effects/gib1.ogg");

    public readonly List<EntProtoId> WerewolfActions = new()
    {
        "ActionWerewolfTransfurm",
        "ActionWerewolfOpenMutationStore",
        "ActionWerewolfAbsorb",
        "ActionWerewolfHowl"
    };

    [DataField, AutoNetworkedField]
    public bool Transfurmed;

    [DataField]
    public bool StoreOpened = true; // todo werewolf ungoida it, tie it to the mind and not the body you chud i fucking hate you future me raagh
    // fuck you piece of shit previous me, why the fuck are half of the shit broken
    // fuck you both why the fuck did the ww use changeling rule?? why did you let that pass you fucking chud previous me - dr. autism APR 28 2026

    [DataField, AutoNetworkedField]
    public ProtoId<PolymorphPrototype> CurrentMutation;

    /// <summary>
    /// Amount of points given per devour action performed of a person
    /// </summary>
    [DataField]
    public int AmountDevour = 2;

    /// <summary>
    /// Amount of points given per gut action performed
    /// </summary>
    [DataField]
    public int AmountGut = 1;

    [DataField]
    public ProtoId<CollectiveMindPrototype> CollectiveMindChannel = "LunarMind";
}
