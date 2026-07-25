using System.Linq;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Inky.Shared.Werewolf.EntityEffects;

/// <summary>
/// Amputates a limb from an entity if it has one.
/// </summary>
public sealed partial class AmputateLimb : EntityEffectBase<AmputateLimb>
{
    [DataField(required: true)]
    public string LimbName { get; set; } = string.Empty;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class AmputateLimbEffectSystem : EntityEffectSystem<MetaDataComponent, AmputateLimb>
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private WoundSystem _wound = default!;
    [Dependency] private IRobustRandom _random = default!;

    protected override void Effect(Entity<MetaDataComponent> ent, ref EntityEffectEvent<AmputateLimb> args) // yes this is a copypaste from sharedwerewolfbasicabilitiessystem kill me todo werewolf
    {
        if (!TryComp<BodyComponent>(ent, out var body))
            return;

        var targetLimb = args.Effect.LimbName;

        var allOrgans = _body.GetOrgans((ent, body));
        var limbs = allOrgans
            .Where(organ =>
            {
                var category = _body.GetCategory(new Entity<OrganComponent?>(organ.Owner, organ.Comp));
                return category == targetLimb;
            })
            .ToList();

        if (limbs.Count <= 0)
            return;

        var pick = _random.Next(limbs.Count); // in case if someone has two or more of this bodypart, remove a random one
        var picked = limbs[pick];

        if (!TryComp<WoundableComponent>(picked.Owner, out var wound)
            || !wound.ParentWoundable.HasValue)
            return;

        _wound.AmputateWoundableSafely(wound.ParentWoundable.Value, picked.Owner, wound);
    }
}
