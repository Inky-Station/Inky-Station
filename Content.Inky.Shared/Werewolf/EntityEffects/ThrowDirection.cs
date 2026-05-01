using Content.Shared.EntityEffects;
using Content.Shared.Throwing;
using Robust.Shared.Prototypes;

namespace Content.Inky.Shared.Werewolf.EntityEffects;

/// <summary>
/// Throws the target entity away related to the user into the oposite dirrection
/// </summary>
public sealed partial class ThrowDirection : EntityEffectBase<ThrowDirection>
{
    [DataField]
    public float Speed = 10f;

    [DataField]
    public bool Predicted = true;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed class ThrowDirectionEffectSystem : EntityEffectSystem<MetaDataComponent, ThrowDirection>
{
    [Dependency] private readonly ThrowingSystem _JOHNCENA = default!;

    protected override void Effect(Entity<MetaDataComponent> ent, ref EntityEffectEvent<ThrowDirection> args)
    {
        if (args.User is null)
            return;

        var userPos = Transform(args.User.Value).WorldPosition;
        var victimPos = Transform(ent).WorldPosition;

        var target = (victimPos - userPos).Normalized();

        var effect = args.Effect;
        _JOHNCENA.TryThrow(ent,
            -target,
            baseThrowSpeed: effect.Speed,
            user: args.User,
            predicted: effect.Predicted);
    }
}
