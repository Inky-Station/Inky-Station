using Content.Inky.Common.Misc;
using Content.Goobstation.Shared.JumpScare;
using Content.Goobstation.Shared.Projectiles;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Inky.Shared.Misc;

// SYSTEM O' FUN!!!!
public sealed partial class FlipOffSystem : EntitySystem
{
    [Dependency] private IFullScreenImageJumpscare _jumpscare = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private INetManager _net = default!;

    private const string Sound = "/Audio/_Inky/Shitpost/boom.ogg";
    private const string John = "/Textures/_Inky/im not gonna syndiecoat it.png";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FlipDoStatusEffectComponent, ProjectileParriedEvent>(OnProjectileParried);
    }

    private void OnProjectileParried(Entity<FlipDoStatusEffectComponent> ent, ref ProjectileParriedEvent args)
    {
        if (!_net.IsServer
            || !HasComp<SpecialFlipParryHolderComponent>(ent.Owner)
            || !_player.TryGetSessionByEntity(ent.Owner, out var session))
            return;

        _jumpscare.Jumpscare(new SpriteSpecifier.Texture(new ResPath(John)), session);
        _audio.PlayGlobal(new SoundPathSpecifier(Sound), session);
    }
}
