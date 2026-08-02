using Content.Client.Overlays;
using Content.Inky.Common.Medical;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Rejuvenate;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Inky.Client.Inkymed;

public sealed partial class BloodlossOverlaySystem : EntitySystem
{
    [Dependency] private IPlayerManager _playerMan = default!;
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private DamageableSystem _damageableSystem = default!;
    [Dependency] private EntityQuery<BloodstreamComponent> _stream = default!;

    private BlackAndWhiteOverlay _overlay = default!;

    public override void Initialize()
    {
        _overlay = new BlackAndWhiteOverlay();
        _overlay.Intensity = 0f;
        _overlayMan.AddOverlay(_overlay);

        SubscribeLocalEvent<BloodstreamComponent, UpdateBloodstreamOverlayEvent>(OnUpdate);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(args => Refresh(args.Entity));
    }

    private void OnUpdate(Entity<BloodstreamComponent> ent, ref UpdateBloodstreamOverlayEvent args)
    {
        if (ent.Owner == _playerMan.LocalEntity)
            Refresh(ent.Owner);
    }

    private void Refresh(EntityUid player)
    {
        if (player != _playerMan.LocalEntity
            || !_stream.TryComp(player, out _))
        {
            _overlay.Intensity = 0f;
            return;
        }

        var damage = _damageableSystem.GetAllDamage(player);
        if (!damage.DamageDict.TryGetValue("Bloodloss", out var bloodlossDamage))
        {
            _overlay.Intensity = 0f;
            return;
        }

        _overlay.Intensity = Math.Clamp(bloodlossDamage.Float() / 100f, 0f, 1f);
    }
}
