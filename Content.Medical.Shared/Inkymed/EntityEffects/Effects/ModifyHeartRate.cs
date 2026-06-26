using Content.Shared.EntityEffects;

namespace Content.Medical.Shared.Inkymed.EntityEffects.Effects;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ModifyHeartRate : EntityEffectBase<ModifyHeartRate>
{
    /// <summary>
    /// amount to add to the currentheartrate per metabolism tick
    /// negatives values slow the heard down, positive valued speed it up
    /// </summary>
    [DataField(required: true)]
    public float Amount;

    /// <summary>
    /// should the reagent ignore the stopped heart?
    /// this will restart the heart if it were at zero.
    /// </summary>
    [DataField]
    public bool HeartRestart;

    /// <summary>
    /// If true, the amount specified will try to go towards the startingheartrate
    /// if the amount specified is negative it will try to de-stabilise it instead
    /// </summary>
    [DataField]
    public bool AutoStabilisation;

    /// <summary>
    /// If not null, the reagent will not lower the heart rate below this value
    /// </summary>
    [DataField]
    public float? LowerCap;

    /// <summary>
    /// If not null, the reagent will not raise the heart rate above this value
    /// </summary>
    [DataField]
    public float? HigherCap;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) // todo this is pure SLOP
    {
        var lines = new List<string>();

        if (AutoStabilisation)
        {
            if (Amount >= 0)
            {
                lines.Add(Loc.GetString("entity-effect-guidebook-modify-heart-rate-stabilise-increase",
                    ("amount", Math.Abs(Amount)),
                    ("highCap", HigherCap ?? 80))); // todo inkymed pass startingheartrate somehow
                lines.Add(Loc.GetString("entity-effect-guidebook-modify-heart-rate-stabilise-decrease",
                    ("amount", Math.Abs(Amount)),
                    ("lowCap", LowerCap ?? 80)));
            }
        }
        else
        {
            var key = Amount >= 0
                ? "entity-effect-guidebook-modify-heart-rate-increase"
                : "entity-effect-guidebook-modify-heart-rate-decrease";
            lines.Add(Loc.GetString(key, ("amount", Math.Abs(Amount))));
        }

        if (HeartRestart)
            lines.Add(Loc.GetString("entity-effect-guidebook-modify-heart-rate-restart"));

        return string.Join("\n", lines); // i hate fucking locale
    }
}
