using Robust.Shared.Serialization;
namespace Content.Inky.Common.Whale;

[Serializable, NetSerializable]
public sealed class LeviathanMusicStartEvent : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class LeviathanMusicStopEvent : EntityEventArgs;


public sealed class GrappleEmbedCompletedEvent : EntityEventArgs // hate.
{
    public readonly EntityUid Embedded;
    public readonly EntityUid Weapon;
    public readonly EntityUid? Shooter;

    public GrappleEmbedCompletedEvent(EntityUid embedded, EntityUid weapon, EntityUid? shooter)
    {
        Embedded = embedded;
        Weapon = weapon;
        Shooter = shooter;
    }
}
