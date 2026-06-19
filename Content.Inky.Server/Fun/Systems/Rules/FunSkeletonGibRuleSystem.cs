using Content.Inky.Server.Fun.Components.Rules;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Server.Polymorph.Systems;
using Content.Shared.Gibbing;
using Content.Shared.Humanoid;
using Robust.Server.Audio;
using Robust.Shared.Audio;

namespace Content.Inky.Server.Fun.Systems.Rules;

public sealed class FunSkeletonGibSystem : GameRuleSystem<FunSkeletonGibRuleComponent>
{
    [Dependency] private readonly FunnyThingsSystem _fun = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    private readonly SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_Goobstation/Voice/badtothebone.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidProfileComponent, BeingGibbedEvent>(OnGibbed);
    }

    private void OnGibbed(EntityUid uid, HumanoidProfileComponent comp, ref BeingGibbedEvent args)
    {
        if (!_fun.CheckRule<FunSkeletonGibRuleComponent>())
            return;

        if (!_mind.TryGetMind(uid, out var mindId, out var mindComp))
            return;
        _mind.TransferTo(mindId, null, mind: mindComp); // this is partially copied from bindsoul, and this is here because the brain contains the mind and is dropped on polymorph

        var skeleton = _polymorph.PolymorphEntity(uid, "FunSkeleton");
        if (skeleton == null)
            return;

        _mind.TransferTo(mindId, skeleton, mind: mindComp);
        _audio.PlayPvs(Sound, skeleton.Value);
    }
}
