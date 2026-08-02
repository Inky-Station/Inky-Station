using System.Text;
using Content.Inky.Shared.Werewolf;
using Content.Inky.Shared.Werewolf.Components;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Shared.Mind;
using Content.Server.Objectives;
using Content.Shared.Actions;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.Overlays;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Inky.Server.Werewolf.Systems;

public sealed partial class WerewolfRuleSystem : GameRuleSystem<WerewolfRuleComponent>
{
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private ObjectivesSystem _objectives = default!;

    public readonly SoundSpecifier BriefingSound = new SoundPathSpecifier("/Audio/_Inky/Antag/Werewolf/werewolf_start.ogg");

    public readonly ProtoId<CurrencyPrototype> Currency = "Fury";

    public readonly int StartingCurrency = 2; // to buy either regen or ambush, choose your game

    public readonly EntProtoId MindRole = "MindRoleWerewolf";

    public readonly ProtoId<EntityEffectPrototype> WerewolfSkills = "WerewolfSkills";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WerewolfRuleComponent, AfterAntagEntitySelectedEvent>(OnSelectAntag);
        SubscribeLocalEvent<WerewolfRuleComponent, ObjectivesTextPrependEvent>(OnTextPrepend);

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

        _role.MindAddRole(mindId, MindRole.Id, mind, true);

        var briefing = Loc.GetString("werewolf-role-greeting");
        var briefingShort = Loc.GetString("werewolf-role-greeting-short");

        if (_role.MindHasRole<WerewolfRuleComponent>(mindId, out var mindRole))
            AddComp(mindRole.Value, new RoleBriefingComponent { Briefing = briefingShort }, overwrite: true);

        EnsureComp<WerewolfAbilitiesComponent>(target, out var werewolfComp);
        EnsureComp<WerewolfMindComponent>(mindId);

        foreach (var action in werewolfComp.WerewolfActions)
        {
            _actions.AddAction(target, action, container: mindId);
        }

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

        // GOIDA
        EnsureComp<NightVisionComponent>(target).LightingColor = Color.FromHex("#303030");

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

    private void OnTextPrepend(Entity<WerewolfRuleComponent> ent, ref ObjectivesTextPrependEvent args)
    {
        var sb = new StringBuilder();

        var eqe = EntityQueryEnumerator<WerewolfMindComponent, MindComponent>();
        while (eqe.MoveNext(out var mindId, out var werewolf, out var mind))
        {
            var name = _objectives.GetTitle((mindId, mind), Name(mind.OwnedEntity ?? mindId));
            sb.AppendLine($"{name} bit [color=red]{werewolf.BittenPeople.Count}[/color] people."); // idfc

            if (!werewolf.PackMembers.Any())
                continue;

            var pack = new List<string>();
            foreach (var packMind in werewolf.PackMembers)
            {
                if (!TryComp<MindComponent>(packMind, out var packMind1))
                    continue;

                pack.Add(_objectives.GetTitle((packMind, packMind1), Name(packMind1.OwnedEntity ?? packMind)));
            }

            sb.AppendLine($"{name}'s pack: {string.Join(", ", pack)}.");
        }

        args.Text = sb.ToString();
    }
}
