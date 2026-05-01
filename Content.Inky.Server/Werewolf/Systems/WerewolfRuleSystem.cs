using System.Linq;
using Content.Inky.Shared.Werewolf.Components;
using Content.Inky.Shared.Werewolf.Systems;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Inky.Server.Werewolf.Systems;

public sealed class WerewolfRuleSystem : GameRuleSystem<WerewolfRuleComponent>
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly SharedWerewolfBasicAbilitiesSystem _werewolf = default!; // hell.

    public readonly SoundSpecifier BriefingSound = new SoundPathSpecifier("/Audio/_Inky/Antag/Werewolf/werewolf_start.ogg");

    public readonly ProtoId<AntagPrototype> WerewolfPrototypeId = "Werewolf";

    public readonly ProtoId<CurrencyPrototype> Currency = "Fury";

    public readonly int StartingCurrency = 2; // to buy either regen or ambush, choose your game

    [ValidatePrototypeId<EntityPrototype>] EntProtoId mindRole = "MindRoleWerewolf";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WerewolfRuleComponent, AfterAntagEntitySelectedEvent>(OnSelectAntag);
        // SubscribeLocalEvent<WerewolfRuleComponent, ObjectivesTextPrependEvent>(OnTextPrepend);
    }

    private void OnSelectAntag(EntityUid uid, WerewolfRuleComponent comp, ref AfterAntagEntitySelectedEvent args)
    {
        MakeWerewolf(args.EntityUid, comp);
    }

    public bool MakeWerewolf(EntityUid target, WerewolfRuleComponent rule)
    {
        if (!_mind.TryGetMind(target, out var mindId, out var mind))
            return false;

        _role.MindAddRole(mindId, mindRole.Id, mind, true);

        var briefing = Loc.GetString("werewolf-role-greeting");
        var briefingShort = Loc.GetString("werewolf-role-greeting-short");

        if (_role.MindHasRole<WerewolfRuleComponent>(mindId, out var mr))
                AddComp(mr.Value, new RoleBriefingComponent { Briefing = briefingShort }, overwrite: true);

        EnsureComp<WerewolfBasicAbilitiesComponent>(target, out var werewolfComp);
        EnsureComp<WerewolfMindComponent>(mindId, out var werewolfMind);

        werewolfMind.UnlockedActions = werewolfComp.WerewolfActions.Select(id => (string)id).ToList(); // add the actions to the werewolf mind (polymorph shitcode)
        _werewolf.SyncActions(target, werewolfComp);

        // add store
        var store = EnsureComp<StoreComponent>(target);
        foreach (var category in rule.StoreCategories)
            store.Categories.Add(category);
        store.CurrencyWhitelist.Add(Currency);
        store.Balance.Add(Currency, StartingCurrency);

        rule.WerewolfMinds.Add(mindId);
        _antag.SendBriefing(target, briefing, Color.Brown, BriefingSound);
        return true;
    }

    // todo OnTextPrepend
}
