using Content.Inky.Server.Fun.Components.Rules;
using Content.Server.GameTicking.Rules;
using Content.Shared.Humanoid;
using Content.Shared.Shuttles.Components;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Map;

namespace Content.Inky.Server.Fun.Systems.Rules;

public sealed class WilhelmFtlFunRuleSystem : GameRuleSystem<WilhelmFtlFunRuleComponent>
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly FunnyThingsSystem _fun = default!;

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
        var mapEntity = _mapMan.GetMapEntityId(mapId);

        if (!HasComp<FTLMapComponent>(mapEntity))
            return;

        if (args.Transform.ParentUid != mapEntity)
            return;

        _audio.PlayPvs(Sound, uid);
    }
}
