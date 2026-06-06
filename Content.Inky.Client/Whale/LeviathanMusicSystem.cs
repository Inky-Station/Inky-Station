using Content.Client.Audio;
using Content.Inky.Common.Whale;
using Content.Shared.GameTicking;
using Content.Shared.Mobs;
using Robust.Client.Audio;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Player;

namespace Content.Inky.Client.Whale;

public sealed class LeviathanMusicSystem : EntitySystem // i tried to use bossmusicsystem, its fucking horrible.
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private ContentAudioSystem _audioContent = default!;

    private Entity<AudioComponent?>? _stream;
    private bool _fadingOut;

    private const float FadeOutDuration = 3f;
    private const float MinVolume = -32f;
    private float _fadeVolumePerSecond;
    private float _startVolume;

    private static readonly SoundPathSpecifier Music = new("/Audio/_Inky/Mobs/Bosses/spacecalls.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<LeviathanMusicStartEvent>(OnStart);
        SubscribeNetworkEvent<LeviathanMusicStopEvent>(OnStop);

        SubscribeLocalEvent<LocalPlayerDetachedEvent>(_ => StopMusic());
        SubscribeLocalEvent<RoundEndMessageEvent>(_ => StopMusic());
        // SubscribeLocalEvent<ActorComponent, MobStateChangedEvent>(OnPlayerDeath);
        // SubscribeLocalEvent<ActorComponent, EntParentChangedMessage>(OnPlayerParentChange);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        StopMusic();
    }

    private void OnStart(LeviathanMusicStartEvent _)
    {
        if (_stream != null)
            return;

        _audioContent.DisableAmbientMusic();

        var stream = _audio.PlayGlobal(
            Music,
            Filter.Local(),
            false,
            AudioParams.Default.WithLoop(true));

        if (stream != null)
            _stream = (stream.Value.Entity, stream.Value.Component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_fadingOut
            || _stream == null)
            return;

        if (!TryComp(_stream.Value.Owner, out AudioComponent? component))
        {
            _fadingOut = false;
            _stream = null;
            return;
        }

        var volume = component.Volume - _fadeVolumePerSecond * frameTime;
        volume = MathF.Max(MinVolume, volume);
        _audio.SetVolume(_stream.Value.Owner, volume, component);

        if (component.Volume <= MinVolume)
        {
            _stream = _audio.Stop(_stream);
            _fadingOut = false;
        }
    }

    private void OnStop(LeviathanMusicStopEvent _)
        => BeginFadeOut();

    private void BeginFadeOut()
    {
        if (_stream == null)
            return;

        if (!TryComp(_stream.Value.Owner, out AudioComponent? component))
        {
            StopMusic();
            return;
        }

        _startVolume = component.Volume;
        _fadeVolumePerSecond = (_startVolume - MinVolume) / FadeOutDuration;
        _fadingOut = true;
    }

    private void StopMusic()
    {
        _fadingOut = false;
        _stream = _audio.Stop(_stream);
    }
}
