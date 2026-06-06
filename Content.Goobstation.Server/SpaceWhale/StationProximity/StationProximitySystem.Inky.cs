using Content.Goobstation.Server.MobCaller;
using Content.Inky.Common.Whale;
using Content.Shared.Mobs;
using Robust.Shared.Player;

namespace Content.Goobstation.Server.SpaceWhale.StationProximity;

public sealed partial class StationProximitySystem
{
    public void InitializeInky()
    {
        SubscribeLocalEvent<SpaceLeviathanComponent, MapInitEvent>(OnLeviathanSpawned);
        SubscribeLocalEvent<SpaceLeviathanComponent, MobStateChangedEvent>(OnWhaleDeath);
        // SubscribeLocalEvent<SpaceWhaleTargetComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<SpaceLeviathanComponent, EntityTerminatingEvent>(OnLeviathanDeleted);
    }

    private void OnLeviathanDeleted(Entity<SpaceLeviathanComponent> ent, ref EntityTerminatingEvent args)
        => StopAllMusic();

    private void OnWhaleDeath(Entity<SpaceLeviathanComponent> ent, ref MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Alive)
            return;

        StopAllMusic();
    }

    private void OnLeviathanSpawned(Entity<SpaceLeviathanComponent> ent, ref MapInitEvent args)
    {
        var eqe = EntityQueryEnumerator<SpaceWhaleTargetComponent>();
        while (eqe.MoveNext(out var playerUid, out var target))
        {
            if (!TryComp<MobCallerComponent>(target.MobCaller, out _))
                continue;

            if (!TryComp<ActorComponent>(playerUid, out var actor))
                continue;

            RaiseNetworkEvent(new LeviathanMusicStartEvent(), actor.PlayerSession.Channel); // hate
            return;
        }
    }

    private void StopAllMusic()
    {
        var eqe = EntityQueryEnumerator<SpaceWhaleTargetComponent>();
        while (eqe.MoveNext(out var playerUid, out _))
        {
            if (TryComp<ActorComponent>(playerUid, out var actor))
                RaiseNetworkEvent(new LeviathanMusicStopEvent(), actor.PlayerSession.Channel);
        }
    }

}
