using Content.Goobstation.Common.Blob;
using Content.Goobstation.Server.Changeling.GameTicking.Rules;
using Content.Inky.Shared.Werewolf.Components;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Systems;
using Content.Server.Antag;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.Verbs;
using Content.Trauma.Common.Silicon;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Inky.Server.Administration.Systems;

public sealed partial class InkyAdminVerbSystem
{
    [Dependency] private AntagSelectionSystem _antag = default!;

    private void OnGetAntagVerbs(ref GetAntagVerbsEvent args)
    {
        if (!HasComp<MindContainerComponent>(args.Target) || !TryComp<ActorComponent>(args.Target, out var targetActor))
            return;

        var targetPlayer = targetActor.PlayerSession;

        args.Verbs.Verbs.Add(new()
        {
            Text = Loc.GetString("admin-verb-text-make-werewolf"),
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_Inky/Actions/Werewolf/werewolf.rsi"), "howl"),
            Act = () =>
            {
                _antag.ForceMakeAntag<WerewolfRuleComponent>(targetPlayer, "Werewolf");
            },
            Impact = LogImpact.High,
            Message = Loc.GetString("admin-verb-make-werewolf"),
        });
    }
}
