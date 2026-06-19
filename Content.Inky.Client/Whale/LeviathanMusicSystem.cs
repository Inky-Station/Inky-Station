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
            AudioParams.Default.WithLoop(false));

        if (stream != null)
            _stream = (stream.Value.Entity, stream.Value.Component);
    }

    private void OnStop(LeviathanMusicStopEvent _)
        => StopMusic();

    private void StopMusic()
    {
        _stream = _audio.Stop(_stream);
    }
}
