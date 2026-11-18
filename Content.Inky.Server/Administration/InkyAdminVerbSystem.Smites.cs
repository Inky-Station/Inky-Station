using Content.Inky.Common.Medical;
using Content.Medical.Common.Body;
using Content.Shared.Administration;
using Content.Shared.Body;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Inky.Server.Administration;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class InkyAdminVerbSystem
{
    private static readonly ProtoId<OrganCategoryPrototype> Brain = "Brain";
    /// <inheritdoc/>
    public void InitializeSmites() { }

    [SubscribeLocalEvent]
    private void AddSmiteVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp(args.User, out ActorComponent? actor)
            || !_admin.HasAdminFlag(actor.PlayerSession, AdminFlags.Fun)
            || HasComp<MapComponent>(args.Target)
            || HasComp<MapGridComponent>(args.Target))
            return;

        Verb autismspectrumdisorder = new()
        {
            Text = Loc.GetString("admin-verbs-smite-autism-name".ToLowerInvariant()),
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/_Inky/Interface/Inkymed/alerts.rsi"), "autism"),
            Act = () =>
            {
                if (_body.GetOrgan(args.Target, Brain) is { } brain)
                    EnsureComp<AutismComponent>(brain);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-verbs-smite-autism-desc"),
        };
        args.Verbs.Add(autismspectrumdisorder);
    }
}
