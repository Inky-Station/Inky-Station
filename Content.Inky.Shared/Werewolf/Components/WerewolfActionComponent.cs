using Robust.Shared.GameStates;

namespace Content.Inky.Shared.Werewolf.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class WerewolfActionComponent : Component
{

    [DataField]
    public float HungerCost = 30f;

    [DataField]
    public bool RequireTransfurmed = false;

    [DataField]
    public bool OneTimeUse;

    [DataField]
    public LocId NotTransfurmedPopup = "werewolf-action-fail-transfurmed";

    [DataField]
    public LocId NoHungerPopup = "werewolf-action-fail-hunger";
}
