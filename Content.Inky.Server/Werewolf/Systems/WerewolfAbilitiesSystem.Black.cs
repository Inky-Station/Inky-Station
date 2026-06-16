using Content.Inky.Shared.Werewolf;
using Content.Inky.Shared.Werewolf.Components;
using Content.Shared.Chat;
using Robust.Shared.Utility;

namespace Content.Inky.Server.Werewolf.Systems;

public sealed partial class WerewolfAbilitiesSystem
{
    /// <inheritdoc/>
    public void InitializeBlack()
    {
        SubscribeLocalEvent<WerewolfAbilitiesComponent, WerewolfBeckonEvent>(OnBeckon);
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
}
