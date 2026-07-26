using System.Linq;
using Content.Inky.Shared.Werewolf;
using Content.Inky.Shared.Werewolf.Components;
using Content.Shared.Chat;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Robust.Shared.Utility;

namespace Content.Inky.Server.Werewolf.Systems;

public sealed partial class WerewolfAbilitiesSystem
{
    /// <inheritdoc/>
    public void InitializeBlack()
    {
        SubscribeLocalEvent<WerewolfAbilitiesComponent, WerewolfBeckonEvent>(OnBeckon);
        SubscribeLocalEvent<WerewolfAbilitiesComponent, WerewolfBlackCallEvent>(OnCall);
    }

    private void OnBeckon(EntityUid uid, WerewolfAbilitiesComponent comp, WerewolfBeckonEvent args)
    {
        var locationName = FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString(uid));

        var message = Loc.GetString("werewolf-beckon-message",
            ("name", MetaData(uid).EntityName),
            ("location", locationName));

        _chat.TrySendInGameICMessage(uid, $"+l {message}", InGameICChatType.CollectiveMind, ChatTransmitRange.Normal); // holy goida IF ANYONE CHANGES LUNARMIND KEY LETTER CHANGE IT HERE TOO
        args.Handled = true;
    }

    private void OnCall(EntityUid uid, WerewolfAbilitiesComponent comp, WerewolfBlackCallEvent args)
    {
        if (!_mind.TryGetMind(uid, out var leaderMind, out _)
            || !TryComp<WerewolfMindComponent>(leaderMind, out var leaderMindTakeTwo))
            return;

        var alphas = new List<(EntityUid Mind, EntityUid Body)> { (leaderMind, uid) };
        var alphasMind = new HashSet<EntityUid> { leaderMind }; // has to be hashset bcuz bullshit

        foreach (var alphaMind in leaderMindTakeTwo.PackMembers)
        {
            if (!alphasMind.Add(alphaMind)
                || !TryComp<MindComponent>(alphaMind, out var alphaMindIdk)
                || alphaMindIdk.OwnedEntity is not { } alphaBody
                || !HasComp<WerewolfAbilitiesComponent>(alphaBody))
                continue;

            alphas.Add((alphaMind, alphaBody));
        }

        // The original alpha needs to have 4 more alphas that hit the gym EVERY DAY to be on that grindset to do the call
        if (alphas.Count < 5)
        {
            _popup.PopupClient(Loc.GetString("werewolf-black-call-fail-amount"), uid);
            return;
        }

        foreach (var (wolfMindId, wolfBody) in alphas)
        {
            if (TryComp<WerewolfAbilitiesComponent>(wolfBody, out var wolfAbilities)
                && !wolfAbilities.Transfurmed)
            {
                RaiseLocalEvent(wolfBody, new TransfurmEvent(true));
            }

            if (!TryComp<WerewolfMindComponent>(wolfMindId, out var wolfMind)
                || !TryComp<MindComponent>(wolfMindId, out var mind)
                || mind.OwnedEntity is not { } transformedBody)
                continue;

            wolfMind.BlockTransfurm = true;

            if (!TryComp<MobStateComponent>(transformedBody, out _)
                || !TryComp<MobThresholdsComponent>(transformedBody, out var thresholds))
                continue;

            foreach (var (health, state) in thresholds.Thresholds.ToArray())
            {
                _mobThresholds.SetMobStateThreshold(transformedBody, health * 2, state, thresholds);
            }
        }

        if (_station.GetOwningStation(uid) is { } station)
            _stationAlerts.SetLevel(station, "violet", true, true, true); // on a side note why the fuck is this shit not capitalised

        var message = Loc.GetString("werewolf-black-call-success");
        _chat.TrySendInGameICMessage(uid, $"+l {message}", InGameICChatType.CollectiveMind, ChatTransmitRange.Normal);
        args.Handled = true;
        RaiseLocalEvent(uid, new WerewolfActionRemoveEvent(args.Action)); // kill yourself
    }
}
