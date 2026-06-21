using Content.Medical.Shared.Body;
using Content.Medical.Shared.Inkymed.EntityEffects.Effects;
using Content.Shared.Body;
using Content.Shared.EntityEffects;

namespace Content.Medical.Server.Inkymed.EntityEffects.Effects;

public sealed partial class ModifyHeartRateSystem : EntityEffectSystem<BodyComponent, ModifyHeartRate>
{
    [Dependency] private HeartSystem _heart = default!;
    [Dependency] private BodySystem _body = default!;

    public static readonly ProtoId<OrganCategoryPrototype> HeartCategory = "Heart";

    protected override void Effect(Entity<BodyComponent> entity, ref EntityEffectEvent<ModifyHeartRate> args)
    {
        var heartUid = _body.GetOrgan(entity, HeartCategory);
        if (heartUid == null
            || !TryComp<HeartComponent>(heartUid, out var heart))
            return;

        int delta; // HeartSystem copypaste with tweaks
        if (args.Effect.AutoStabilisation)
        {
            /*
             * positive amount = pushing towards startingheartrate
             * negative amount = pushing away from startingheartrate
             */
            var towardTarget = heart.CurrentHeartRate < heart.StartingHeartRate
                ? 1
                : -1;
            var sign = args.Effect.Amount >= 0 ? towardTarget : -towardTarget;
            delta = (int)Math.Round(Math.Abs(args.Effect.Amount) * args.Scale) * sign;
        }
        else
        {
            delta = (int)Math.Round(args.Effect.Amount * args.Scale);
        }

        _heart.SetHeartRate(heartUid.Value, heart, heart.CurrentHeartRate + delta, args.Effect.HeartRestart);
    }
}
