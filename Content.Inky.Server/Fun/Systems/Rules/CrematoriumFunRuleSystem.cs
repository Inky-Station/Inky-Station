using Content.Inky.Server.Fun.Components.Rules;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Rules;
using Content.Shared.Chat;
using Content.Shared.Dataset;
using Content.Shared.Morgue;
using Content.Shared.Morgue.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Inky.Server.Fun.Systems.Rules;

public sealed class CrematoriumFunRuleSystem : GameRuleSystem<CrematoriumFunRuleComponent>
{
    [Dependency] private readonly IRobustRandom _gambling = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly FunnyThingsSystem _fun = default!;

    private const float CancelProb = 0.8f; // todo put this inside a gamerule idk
    private static readonly ProtoId<LocalizedDatasetPrototype> Dataset = "CrematoriumFun";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveCrematoriumComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, ActiveCrematoriumComponent comp, ComponentInit args)
    {
        if (!_fun.CheckRule<CrematoriumFunRuleComponent>())
            return;

        if (_gambling.Prob(CancelProb))
            CancelCremation(uid);
        else
            _chat.TrySendInGameICMessage(uid, Loc.GetString("crematorium-fun-success"), InGameICChatType.Speak, true);
    }

    private void CancelCremation(EntityUid uid)
    {
        if (!TryComp<ActiveCrematoriumComponent>(uid, out _))
            return;
        RemComp<ActiveCrematoriumComponent>(uid);
        _appearance.SetData(uid, CrematoriumVisuals.Burning, false);
        var dataset = _protoMan.Index(Dataset);
        _chat.TrySendInGameICMessage(uid, Loc.GetString(_gambling.Pick(dataset.Values)), InGameICChatType.Speak, true);
    }
}
