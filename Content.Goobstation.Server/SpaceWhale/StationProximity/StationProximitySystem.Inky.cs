using Content.Goobstation.Server.MobCaller;
using Content.Inky.Common.Whale;
using Content.Server.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Player;

namespace Content.Goobstation.Server.SpaceWhale.StationProximity;

public sealed partial class StationProximitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    private const float AdrenalineAmount = 60f;

    public void InitializeInky()
    {
        SubscribeLocalEvent<SpaceLeviathanComponent, MapInitEvent>(OnLeviathanSpawned);
        SubscribeLocalEvent<SpaceLeviathanComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SpaceLeviathanComponent, MobStateChangedEvent>(OnWhaleDeath);
        // SubscribeLocalEvent<SpaceWhaleTargetComponent, EntParentChangedMessage>(OnParentChanged);
    }

    private void OnShutdown(Entity<SpaceLeviathanComponent> entity, ref ComponentShutdown args)
        => StopAllMusic();

    private void OnWhaleDeath(Entity<SpaceLeviathanComponent> ent, ref MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Alive)
            return;

        StopAllMusic();
        foreach (var target in ent.Comp.Targets)
        {
            if (target == null)
                continue;

            StopFollowing(target.Value);
            RemComp<SpaceWhaleTargetComponent>(target.Value);
        }
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

            // adrenaline stuff
            var solution = new Solution();
            solution.AddReagent(new ReagentId("Adrenaline", null), AdrenalineAmount);
            if (!_solution.TryGetInjectableSolution(playerUid, out var targetSolution, out _))
                return;

            _solution.TryAddSolution(targetSolution.Value, solution);
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

    private bool AnyLeviathan()
    {
        var eqe = EntityQueryEnumerator<SpaceLeviathanComponent>();
        return eqe.MoveNext(out _, out _);
    }

}
