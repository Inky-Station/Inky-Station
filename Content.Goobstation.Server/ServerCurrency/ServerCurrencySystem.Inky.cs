using Content.Server.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Players;
using Content.Shared.Roles.Jobs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.Goobstation.Server.ServerCurrency;

public sealed partial class ServerCurrencySystem
{
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedJobSystem _jobs = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private SharedPlayerSystem _playerSystem = default!;

    private int _goobcoinsNonAntagMultiplier = 1;
    private int _goobcoinsServerMultiplier = 1;
    private int _goobcoinsMinPlayers = 0;


    public void InitializeInky()
    {
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndTextAppend);
    }

    private void OnRoundEndTextAppend(RoundEndTextAppendEvent ev)
        {
            if (_players.PlayerCount < _goobcoinsMinPlayers) // 0 default unless changed
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
                        {
                            money += GetJobGoobcoins(session);
                            if (!_jobs.CanBeAntag(session))
                                money *= _goobcoinsNonAntagMultiplier;
                        }

                        if (_goobcoinsServerMultiplier != 1)
                            money *= _goobcoinsServerMultiplier;

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
