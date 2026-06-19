using Content.Inky.Common.Whale;
using Content.Server.NPC.HTN;
using Content.Shared.Alert;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Misc;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Input;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

namespace Content.Inky.Server.Whale;

public sealed class LeviathanControlSystem : EntitySystem // half of it is taken from PilotedByClothing
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const float TurnSpeed = 1.8f;
    private const float ForwardSpeed = 30f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrapplingProjectileComponent, GrappleEmbedCompletedEvent>(OnGrapple);
        SubscribeLocalEvent<LeviathanHookComponent, EntityTerminatingEvent>(OnHookGone);
        SubscribeLocalEvent<LeviathanPilotedComponent, ComponentShutdown>(OnPilotedShutdown);
    }

    private void OnGrapple(Entity<GrapplingProjectileComponent> hook, ref GrappleEmbedCompletedEvent args)
    {
        if (!args.Shooter.HasValue)
            return;

        if (!TryComp<GrapplingGunComponent>(args.Weapon, out var gun) || !gun.LeviathanControl)
            return;

        if (!HasComp<SpaceLeviathanComponent>(args.Embedded))
            return;

        var hookTag = AddComp<LeviathanHookComponent>(hook);
        hookTag.Leviathan = args.Embedded;
        hookTag.Pilot = args.Shooter.Value;

        StartControl(args.Embedded, args.Shooter.Value, hook);
    }

    private void OnHookGone(Entity<LeviathanHookComponent> hook, ref EntityTerminatingEvent args)
        => EndControl(hook.Comp.Leviathan, hook.Comp.Pilot);

    private void OnPilotedShutdown(Entity<LeviathanPilotedComponent> ent, ref ComponentShutdown args)
        => EndControl(ent.Owner, ent.Comp.Pilot);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var q = EntityQueryEnumerator<LeviathanPilotedComponent, PhysicsComponent, TransformComponent>();
        while (q.MoveNext(out var uid, out var piloted, out var physics, out var xform))
        {
            if (!TryComp<InputMoverComponent>(piloted.Pilot, out var mover))
                continue;

            var turn = 0f;
            if (mover.HeldMoveButtons.HasFlag(MoveButtons.Left))
                turn += TurnSpeed;
            if (mover.HeldMoveButtons.HasFlag(MoveButtons.Right))
                turn -= TurnSpeed;

            if (turn != 0f)
            {
                var newRot = xform.WorldRotation + new Angle(turn * frameTime);
                _transform.SetWorldRotation(uid, newRot);
            }

            var forward = xform.WorldRotation.ToWorldVec();
            _physics.SetLinearVelocity(uid, forward * ForwardSpeed, body: physics);
        }
    }

    private void StartControl(EntityUid leviathan, EntityUid pilot, EntityUid hook)
    {
        var controlled = TryComp<LeviathanPilotedComponent>(leviathan, out var existing);

        ProtoId<HTNCompoundPrototype>? storedTask = controlled
            ? existing!.StoredHTNTask
            : null;

        if (!controlled && TryComp<HTNComponent>(leviathan, out var htn))
        {
            storedTask = htn.RootTask.Task;
            RemComp<HTNComponent>(leviathan);
        }

        var piloted = EnsureComp<LeviathanPilotedComponent>(leviathan);
        piloted.Pilot = pilot;
        piloted.Hook = hook;
        piloted.StoredHTNTask = storedTask;

        EnsureComp<LeviathanPilotComponent>(pilot).Leviathan = leviathan;
    }

    private void EndControl(EntityUid leviathan, EntityUid pilot)
    {
        if (TryComp<LeviathanPilotedComponent>(leviathan, out var piloted) && piloted.StoredHTNTask.HasValue)
        {
            var htn = EnsureComp<HTNComponent>(leviathan);
            htn.RootTask = new HTNCompoundTask { Task = piloted.StoredHTNTask.Value };
        }

        RemCompDeferred<LeviathanPilotedComponent>(leviathan);
        RemCompDeferred<LeviathanPilotComponent>(pilot);
    }
}
