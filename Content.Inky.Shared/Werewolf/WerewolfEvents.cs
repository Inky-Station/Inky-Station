using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Inky.Shared.Werewolf;

public sealed partial class HowlEvent : InstantActionEvent
{
    public float ShriekPower = 2.5f;
    public int StunDuration = 1;

    public bool ForceTransfurm; // fucking goida bro
    public bool HealNearby;
    public bool PackOnly = true;
}

public sealed partial class TransfurmEvent : InstantActionEvent
{
    public bool Forced;

    public TransfurmEvent() { }
    public TransfurmEvent(bool forced)
    {
        Forced = forced;
    }
}

public sealed partial class TransfurmWhiteEvent : InstantActionEvent
{
    public float Radius = 50f;
}

public sealed partial class EventWerewolfOpenStore : InstantActionEvent;
public sealed partial class EventWerewolfDevour : EntityTargetActionEvent;
public sealed partial class EventWerewolfGut : EntityTargetActionEvent;
public sealed partial class EventWerewolfBleedingBite : EntityTargetActionEvent;
public sealed partial class EventWerewolfBlackBite : EntityTargetActionEvent;
public sealed partial class EventWerewolfChangeType : InstantActionEvent
{
    public string WerewolfType;
}

public sealed partial class EventWerewolfRegen : InstantActionEvent;

public sealed partial class WerewolfAmbushActionEvent : WorldTargetActionEvent
{
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
public sealed partial class EventWerewolfUpgradeAbility : InstantActionEvent
{
    public EntProtoId? OldActionId;
    public EntProtoId NewActionId;
}

public sealed class WerewolfPositionQueryEvent : EntityEventArgs
{
    public Dictionary<EntityUid, Vector2> Positions { get; } = [];
}

public sealed partial class WerewolfAddCollectivemind : InstantActionEvent
{
    public LocId? Popup;
}

public sealed partial class WerewolfRevelationEvent : InstantActionEvent;
public sealed partial class WerewolfBlackCallEvent : InstantActionEvent
{
    public int MinimumWolvesToTransform = 5;
    public float HealthModifier = 2;
}

[ByRefEvent]
public readonly record struct WerewolfInfectionFinishedEvent(EntityUid Entity);
public sealed partial class WerewolfBeckonEvent : InstantActionEvent;
public sealed partial class EventWerewolfBequeath : EntityTargetActionEvent;
public sealed class WerewolfActionRemoveEvent(EntityUid actionEnt) : EntityEventArgs
{
    public readonly EntityUid ActionEnt = actionEnt;
}
