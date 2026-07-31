using Content.Inky.Server.Fun.Components.Rules;
using Content.Server.GameTicking.Rules;
using Content.Shared.Humanoid;
using Content.Shared.Shuttles.Components;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Map;

namespace Content.Inky.Server.Fun.Systems.Rules;

public sealed partial class WilhelmFtlFunRuleSystem : GameRuleSystem<WilhelmFtlFunRuleComponent>
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private SharedMapSystem _mapMan = default!;
    [Dependency] private FunnyThingsSystem _fun = default!;

    private static readonly SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Voice/Human/wilhelm_scream.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidProfileComponent, EntParentChangedMessage>(OnParentChanged);
    }

    private void OnParentChanged(EntityUid uid, HumanoidProfileComponent comp, ref EntParentChangedMessage args)
    {
        if (!_fun.CheckRule<WilhelmFtlFunRuleComponent>())
            return;

        var mapId = args.Transform.MapID;
        var mapEntity = _mapMan.GetMapOrInvalid(mapId); // todo NOW inky check if works

        if (!HasComp<FTLMapComponent>(mapEntity))
            return;

        if (args.Transform.ParentUid != mapEntity)
            return;

        _audio.PlayPvs(Sound, uid);
    }
}
