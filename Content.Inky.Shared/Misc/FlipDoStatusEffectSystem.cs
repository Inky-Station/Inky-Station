using Content.Shared.Chat;
using Content.Shared.Damage.Systems;
using Content.Shared.Standing;
using Content.Shared.StatusEffectNew;
using Content.Inky.Common.Misc;
using Content.Shared.Stunnable;
using Content.Trauma.Shared.Knowledge.Systems;

namespace Content.Inky.Shared.Misc;

public sealed partial class FlipDoStatusEffectSystem : EntitySystem
{
    private const string FlipEmote = "Flip";
    private static readonly TimeSpan FlipDuration = TimeSpan.FromMilliseconds(600);

    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private StandingStateSystem _lastManStanding = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FlipDoStatusEffectComponent, EmoteEvent>(OnFlip);
        SubscribeLocalEvent<FlipDoStatusEffectComponent, KnowledgeAddedEvent>(OnKnowledgeAdded);
    }

    private void OnFlip(Entity<FlipDoStatusEffectComponent> ent, ref EmoteEvent args)
    {
        if (!TryComp<StandingStateComponent>(ent.Owner, out var standing)
            || !args.Voluntary
            || args.Emote.ID != FlipEmote
            || _lastManStanding.IsDown(ent.Owner)
            || !standing.Standing
            || HasComp<KnockedDownComponent>(ent.Owner)
            || HasComp<StunnedComponent>(ent.Owner))
            return;

        if (!_status.TryUpdateStatusEffectDuration(ent.Owner, ent.Comp.StatusEffect, FlipDuration))
            return;

        _stamina.TakeStaminaDamage(ent.Owner, ent.Comp.StaminaCost, source: ent);
    }

    private void OnKnowledgeAdded(Entity<FlipDoStatusEffectComponent> ent, ref KnowledgeAddedEvent args)
    {
        EnsureComp<FlipDoStatusEffectComponent>(args.Holder);
        EnsureComp<SpecialFlipParryHolderComponent>(args.Holder); // fucking goida
    }
}
