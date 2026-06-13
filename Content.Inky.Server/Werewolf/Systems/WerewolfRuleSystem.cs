using System.Linq;
using Content.Inky.Shared.Werewolf;
using Content.Inky.Shared.Werewolf.Components;
using Content.Inky.Shared.Werewolf.Systems;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
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
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    public readonly SoundSpecifier BriefingSound = new SoundPathSpecifier("/Audio/_Inky/Antag/Werewolf/werewolf_start.ogg");

    public readonly ProtoId<AntagPrototype> WerewolfPrototypeId = "Werewolf";

    public readonly ProtoId<CurrencyPrototype> Currency = "Fury";

    public readonly int StartingCurrency = 2; // to buy either regen or ambush, choose your game

    [ValidatePrototypeId<EntityPrototype>] EntProtoId mindRole = "MindRoleWerewolf";

    public readonly ProtoId<EntityEffectPrototype> WerewolfSkills = "WerewolfSkills";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WerewolfRuleComponent, AfterAntagEntitySelectedEvent>(OnSelectAntag);
        // SubscribeLocalEvent<WerewolfRuleComponent, ObjectivesTextPrependEvent>(OnTextPrepend);

        SubscribeLocalEvent<WerewolfInfectionFinishedEvent>(OnInfectionFinished); // goida
    }

    private void OnSelectAntag(EntityUid uid, WerewolfRuleComponent comp, ref AfterAntagEntitySelectedEvent args)
    {
        MakeWerewolf(args.EntityUid, comp);
    }

    /// <summary>
    /// Makes the entity into a werewolf.
    /// </summary>
    /// <param name="target">EntityUid of an entity that is going to become a werewolf</param>
    /// <param name="rule">WerewolfRule</param>
    /// <param name="evolution">Can this werewolf evolve into other werewolf types?</param>
    /// <param name="apprentice">Should this werewolf have access to the blackappentice category?</param> // todo werewolf UNFUCK ME
    /// <returns></returns>
    public bool MakeWerewolf(EntityUid target,
        WerewolfRuleComponent rule,
        bool evolution = true, // todo werewolf maybe rename? first thing that came into my mind
        bool apprentice = false)
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
        if (evolution)
        {
            foreach (var category in rule.StoreCategories)
                store.Categories.Add(category);
        }

        if (apprentice)
            store.Categories.Add(rule.StoreApprentice);

        store.Categories.Add(rule.StoreSide); // maybe its better to make its own bool for it too? but if both evo & side is off, then its no point in adding a store at all
        store.CurrencyWhitelist.Add(Currency);
        store.Balance.Add(Currency, StartingCurrency);

        rule.WerewolfMinds.Add(mindId);
        _antag.SendBriefing(target, briefing, Color.Brown, BriefingSound);
        return true;
    }

    private void OnInfectionFinished(ref WerewolfInfectionFinishedEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var rule, out _))
        {
            RemComp<WerewolfBitComponent>(ev.Entity);
            EnsureComp<WerewolfInfectionImmuneComponent>(ev.Entity);
            MakeWerewolf(ev.Entity, rule, false, true);

            _effects.ApplyEffects(ev.Entity, [new NestedEffect { Proto = WerewolfSkills }], predicted: false); // :face_holding_back_tears:

            return;
        }
    }

    // todo OnTextPrepend
}
