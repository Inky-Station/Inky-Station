using Content.Inky.Shared.Werewolf.Components;
using Content.Shared.Atmos.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;

namespace Content.Inky.Shared.Werewolf.Systems;

public sealed partial class SharedWerewolfAbilitiesSystem
{

    [SubscribeLocalEvent]
    private void OnBanish(EntityUid uid, WerewolfAbilitiesComponent comp, WerewolfBanishEvent args)
    {
        if (TryComp<FlammableComponent>(args.Target, out var flammable)
            && flammable.FireStacks > 0) // umhmmmm
        {
            var duration = TimeSpan.FromSeconds(args.Duration * MathF.Max(flammable.FireStacks, 0f));
            _stun.TryUpdateStunDuration(args.Target, duration);
            _stun.TryKnockdown(args.Target, duration);
        }

        args.Handled = true;
    }

    public void UpdateHell() // taken from sharedstarmarksystem, avui pls make it generalised
    {
        if (_net.IsClient)
            return;

        var eqe = EntityQueryEnumerator<SpawnEntitiesOnStepComponent, PhysicsComponent, TransformComponent>();
        while (eqe.MoveNext(out _, out var spawn, out var physics, out var xform))
        {
            if (spawn.NextSpawnTime > _timing.CurTime
                || physics.LinearVelocity.LengthSquared() < spawn.MinimumVelocitySquared)
                continue;

            spawn.NextSpawnTime = _timing.CurTime + spawn.SpawnPeriod;
            Spawn(spawn.Prototype, xform.Coordinates);
        }
    }
}
