using System.Linq;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Inky.Shared.Transparent;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Inky.Client.Transparent;

public sealed partial class InkyTransparentSystem : EntitySystem
{
    private static readonly ProtoId<ShaderPrototype> CheckerShader = "InkyCheckers";
    [Dependency] private EntityQuery<SpriteComponent> _spriteq = default;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<InkyTransparentComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<InkyTransparentComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<InkyTransparentComponent, HeldVisualsUpdatedEvent>(OnHeldVisualsUpdated);
        SubscribeLocalEvent<InkyTransparentComponent, EquipmentVisualsUpdatedEvent>(OnEquipmentVisualsUpdated);
        // idk how to properly remove the shader lol todo inky OnShutdown
    }

    private void OnStartup(Entity<InkyTransparentComponent> ent, ref ComponentStartup args)
    {
        if (_spriteq.TryComp(ent, out var sprite))
            ApplyShader(sprite);
    }

    private void OnAppearanceChange(Entity<InkyTransparentComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite != null)
            ApplyShader(args.Sprite);
    }

    private void OnHeldVisualsUpdated(Entity<InkyTransparentComponent> ent, ref HeldVisualsUpdatedEvent args)
        => ApplyClothing(args.User, args.RevealedLayers);

    private void OnEquipmentVisualsUpdated(Entity<InkyTransparentComponent> ent, ref EquipmentVisualsUpdatedEvent args)
        => ApplyClothing(args.Equipee, args.RevealedLayers);

    private void ApplyClothing(EntityUid wearer, HashSet<string> layers)
    {
        if (layers.Count == 0
            || !_spriteq.TryComp(wearer, out var sprite))
            return;

        foreach (var key in layers)
        {
            if (_sprite.LayerMapTryGet((wearer, sprite), key, out var layer, true))
                sprite.LayerSetShader(layer, CheckerShader);
        }
    }

    private static void ApplyShader(SpriteComponent sprite)
    {
        foreach (var (_, index) in sprite.AllLayers.Select((layer, index) => (layer, index)))
            sprite.LayerSetShader(index, CheckerShader);
    }
}
