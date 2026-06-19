using Content.Inky.Common.CCVar;
using Content.Server.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Players;
using Content.Shared.Roles.Jobs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Goobstation.Server.ServerCurrency;

public sealed partial class ServerCurrencySystem
{
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedJobSystem _jobs = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private SharedPlayerSystem _playerSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private int _minPlayersRequired;
    private int _currencyServerMultiplier; // why is it int...

    public void InitializeInky()
    {
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndTextAppend);

        _minPlayersRequired = _cfg.GetCVar(InkyCVars.CurrencyMinPlayers);
        _currencyServerMultiplier = _cfg.GetCVar(InkyCVars.CurrencyServerMultiplier);
    }

    private void OnRoundEndTextAppend(RoundEndTextAppendEvent ev)
        {
            if (_players.PlayerCount < _minPlayersRequired)
                return;

            var query = EntityQueryEnumerator<MindContainerComponent>();

            while (query.MoveNext(out var uid, out var mindContainer))
            {
                var isBorg = HasComp<BorgChassisComponent>(uid);
                if (!(HasComp<HumanoidProfileComponent>(uid)
                    || HasComp<BorgBrainComponent>(uid)
                    || isBorg))
                    continue;

                if (mindContainer.Mind.HasValue)
                {
                    var mind = Comp<MindComponent>(mindContainer.Mind.Value);
                    if (mind is not null
                        && (isBorg || !_mind.IsCharacterDeadIc(mind)) // Borgs count always as dead so I'll just throw them a bone and give them an exception.
                        && mind.OriginalOwnerUserId.HasValue
                        && _players.TryGetSessionById(mind.UserId, out var session))
                    {
                        int money = 0;
                        if (session is not null)
                            money += GetJobGoobcoins(session);

                        if (_currencyServerMultiplier != 1)
                            money *= _currencyServerMultiplier;

                        // if (session != null && _linkAccount.GetPatron(session)?.Tier != null) // no p2w (for now at least lul)
                        //     money *= 2;

                        _currencyMan.AddCurrency(mind.OriginalOwnerUserId.Value, money);
                    }
                }
            }
        }

    public int GetJobGoobcoins(ICommonSession player)
    {
        if (_playerSystem.ContentData(player) is not { Mind: { } mindId }
            || !_jobs.MindTryGetJob(mindId, out var prototype))
            return 1;

        return prototype.Currency;
    }
}
