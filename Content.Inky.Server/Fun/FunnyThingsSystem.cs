using Content.Inky.Common.CCVar;
using Content.Inky.Server.Fun.Components.Rules;
using Content.Server.GameTicking;
using Robust.Shared.Configuration;
using Robust.Shared.Random;

namespace Content.Inky.Server.Fun;

public sealed class FunnyThingsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IRobustRandom _gambling = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundStartAttemptEvent>(OnRoundStartAttempt);
    }

    private void OnRoundStartAttempt(RoundStartAttemptEvent ev)
    {
        if (ev.Forced) // integration tests force round starts
            return;

        var prob = _cfg.GetCVar(InkyCVars.FunProb);

        var eqe = EntityQueryEnumerator<RoundstartGameruleChooserRuleComponent>();
        while (eqe.MoveNext(out var uid, out var chooser))
        {
            foreach (var rule in chooser.Rules)
            {
                if (!chooser.FunOnly || _gambling.Prob(prob))
                    _gameTicker.AddGameRule(rule);
            }
        }
    }

    public bool CheckRule<T>() where T : Component
    {
        var eqe = EntityQueryEnumerator<T>();
        while (eqe.MoveNext(out _, out _))
            return true;
        return false;
    }
}
