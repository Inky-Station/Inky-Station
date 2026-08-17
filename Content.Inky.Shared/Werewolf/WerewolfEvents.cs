using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Inky.Shared.Werewolf;

public sealed partial class HowlEvent : InstantActionEvent
{
    [DataField]
    public float ShriekPower = 2.5f;
    [DataField]
    public int StunDuration = 1;
    [DataField]
    public bool ForceTransfurm; // fucking goida bro
    [DataField]
    public bool HealNearby;
    [DataField]
    public bool PackOnly = true;
}

public sealed partial class TransfurmEvent : InstantActionEvent
{
    [DataField]
    public bool Forced;

    public TransfurmEvent() { }
    public TransfurmEvent(bool forced)
    {
        Forced = forced;
    }
}

public sealed partial class TransfurmWhiteEvent : InstantActionEvent
{
    [DataField]
    public float Radius = 50f;
}

public sealed partial class WerewolfOpenStoreEvent : InstantActionEvent;
public sealed partial class WerewolfDevourEvent : EntityTargetActionEvent;
public sealed partial class WerewolfGutEvent : EntityTargetActionEvent;
public sealed partial class WerewolfBleedingBiteEvent : EntityTargetActionEvent;
public sealed partial class WerewolfBlackBiteEvent : EntityTargetActionEvent;
public sealed partial class WerewolfChangeTypeEvent : InstantActionEvent
{
    [DataField]
    public string WerewolfType;
}

public sealed partial class WerewolfRegenEvent : InstantActionEvent;

public sealed partial class WerewolfAmbushActionEvent : WorldTargetActionEvent
{
    [DataField]
    public float JumpSpeed = 15f;
}

[Serializable, NetSerializable]
public sealed partial class WerewolfDevourDoAfterEvent : SimpleDoAfterEvent;
[Serializable, NetSerializable]
public sealed partial class WerewolfGutDoAfterEvent : SimpleDoAfterEvent;
[Serializable, NetSerializable]
public sealed partial class WerewolfBleedingBiteDoAfterEvent : SimpleDoAfterEvent;
[Serializable, NetSerializable]
public sealed partial class WerewolfBlackBiteDoAfterEvent : SimpleDoAfterEvent;

// upgrade events idk
// event raised when any werewolf ability is upgraded
// yes this is horrible and probably would be better to replace this with ProductUpgradeId but its kinda shit
public sealed partial class WerewolfUpgradeAbilityEvent : InstantActionEvent
{
    [DataField]
    public EntProtoId? OldActionId;
    [DataField]
    public EntProtoId NewActionId;
}

public sealed class WerewolfPositionQueryEvent : EntityEventArgs
{
    public Dictionary<EntityUid, Vector2> Positions { get; } = [];
}

public sealed partial class WerewolfAddCollectivemindEvent : InstantActionEvent
{
    [DataField]
    public LocId? Popup;
}

public sealed partial class WerewolfRevelationEvent : InstantActionEvent;
public sealed partial class WerewolfBlackCallEvent : InstantActionEvent
{
    [DataField]
    public int MinimumWolvesToTransform = 5;
    [DataField]
    public float HealthModifier = 2;
}

[ByRefEvent]
public readonly record struct WerewolfInfectionFinishedEvent(EntityUid Entity);
public sealed partial class WerewolfBeckonEvent : InstantActionEvent;
public sealed partial class WerewolfBequeathEvent : EntityTargetActionEvent;
public sealed class WerewolfActionRemoveEvent(EntityUid actionEnt) : EntityEventArgs
{
    public readonly EntityUid ActionEnt = actionEnt;
}
