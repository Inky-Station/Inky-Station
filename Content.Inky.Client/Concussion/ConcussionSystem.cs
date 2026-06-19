using Content.Inky.Common.CCVar;
using Content.Inky.Shared.Concussion;
using Robust.Client.Audio;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Inky.Client.Concussion;

public sealed class ConcussionSystem : SharedConcussionSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private static readonly SoundSpecifier ConcussionSound =
        new SoundPathSpecifier("/Audio/_Inky/Ambient/highpitch.ogg");

    private ConcussionState _lastState = ConcussionState.Sane;
    private bool _causeTinitus;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ConcussionThresholdComponent, AfterAutoHandleStateEvent>(OnStateHandled);

        Subs.CVar(_cfg, InkyCVars.ConcussionSound, x => _causeTinitus = x, true);
    }

    private void OnStateHandled(EntityUid uid, ConcussionThresholdComponent comp, ref AfterAutoHandleStateEvent args)
    {
        if (uid != _player.LocalEntity)
            return;

        if (comp.CurrentState == _lastState)
            return; // FUCK YOU
        _lastState = comp.CurrentState;

        if (comp.CurrentState == ConcussionState.Sane)
            return;

        if (_causeTinitus)
            _audio.PlayGlobal(
                ConcussionSound,
                Filter.Local(),
                false,
                AudioParams.Default
                    .WithVolume(-10f));
    }
}
