using Content.Server.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Goobstation.Server.ServerCurrency;

public sealed partial class ServerCurrencySystem
{
    public void InitializeInky()
    {
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
        {
            if (_players.PlayerCount < _goobcoinsMinPlayers) // 0 default unless changed
                return;

            // var lowPopMultiplier = 1.0 - (_players.PlayerCount / (double)_players.MaxPlayers); // no lowpop multiplier for you chuddie

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
                        int money = _goobcoinsPerPlayer;
                        if (session is not null)
                        {
                            money += _jobs.GetJobGoobcoins(session);
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
}
