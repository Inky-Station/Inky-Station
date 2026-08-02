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

        if (_proto.Resolve(comp.CollectiveMindChannel, out var collectiveMind))
            _chat.TrySendInGameICMessage(uid, $"{SharedChatSystem.CollectiveMindPrefix}{collectiveMind.KeyCode} {message}", InGameICChatType.CollectiveMind, ChatTransmitRange.Normal); // holy goida

        args.Handled = true;
    }

    private void OnCall(EntityUid uid, WerewolfAbilitiesComponent comp, WerewolfBlackCallEvent args)
    {
        if (!_mind.TryGetMind(uid, out var leaderMind, out _)
            || !TryComp<WerewolfMindComponent>(leaderMind, out var leaderWerewolfMind))
            return;

        var members = new List<(EntityUid Mind, EntityUid Body)> { (leaderMind, uid) };
        // var memberMinds = new List<EntityUid> { leaderMind };
        foreach (var memberMind in leaderWerewolfMind.PackMembers)
        {
            if (  // memberMinds.Contains(memberMind) ||
                !TryComp<MindComponent>(memberMind, out var memberWerewolfMind)
                || memberWerewolfMind.OwnedEntity is not { } memberBody
                || !HasComp<WerewolfAbilitiesComponent>(memberBody))
                continue;

            ////memberMinds.Add(memberMind);
            members.Add((memberMind, memberBody));
        }

        if (members.Count < args.MinimumWolvesToTransform)
        {
            _popup.PopupClient(Loc.GetString("werewolf-black-call-fail-amount"), uid);
            return;
        }

        foreach (var (wolfMindId, wolfBody) in members)
        {
            if (TryComp<WerewolfAbilitiesComponent>(wolfBody, out var wolfAbilities)
                && !wolfAbilities.Transfurmed)
                RaiseLocalEvent(wolfBody, new TransfurmEvent(true));

            if (!TryComp<WerewolfMindComponent>(wolfMindId, out var wolfMind)
                || !TryComp<MindComponent>(wolfMindId, out var mind)
                || mind.OwnedEntity is not { } transformedBody)
                continue;

            wolfMind.BlockTransfurm = true;

            if (!HasComp<MobStateComponent>(transformedBody)
                || !TryComp<MobThresholdsComponent>(transformedBody, out var thresholds))
                continue;

            foreach (var (health, state) in thresholds.Thresholds.ToArray())
            {
                _mobThresholds.SetMobStateThreshold(transformedBody, health * args.HealthModifier, state, thresholds);
            }
        }

        if (_station.GetOwningStation(uid) is { } station)
            _stationAlerts.SetLevel(station, "violet", true, true, true); // on a side note why the fuck is this shit not capitalised

        var message = Loc.GetString("werewolf-black-call-success");
        if (_proto.Resolve(comp.CollectiveMindChannel, out var collectiveMind))
            _chat.TrySendInGameICMessage(uid, $"{SharedChatSystem.CollectiveMindPrefix}{collectiveMind.KeyCode} {message}", InGameICChatType.CollectiveMind, ChatTransmitRange.Normal);

        args.Handled = true;
        RaiseLocalEvent(uid, new WerewolfActionRemoveEvent(args.Action)); // kill yourself
    }
}
