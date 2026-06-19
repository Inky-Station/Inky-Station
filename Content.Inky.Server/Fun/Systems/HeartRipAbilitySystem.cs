using System.Linq;
using Content.Inky.Server.Fun.Components;
using Content.Inky.Server.Fun.Components.Rules;
using Content.Inky.Shared.Fun.Events;
using Content.Medical.Shared.Body;
using Content.Server.Body.Systems;
using Content.Server.DoAfter;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Robust.Server.Audio;

namespace Content.Inky.Server.Fun.Systems;

public sealed class HeartRipAbilitySystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly FunnyThingsSystem _fun = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HumanoidProfileComponent, GetVerbsEvent<InnateVerb>>(OnGetVerbs);
        SubscribeLocalEvent<HeartRipAbilityComponent, HeartRipDoAfterEvent>(OnDoAfter);
    }

    private void OnGetVerbs(EntityUid uid, HumanoidProfileComponent comp, GetVerbsEvent<InnateVerb> args)
    {
        if (!TryComp<HeartRipAbilityComponent>(args.User, out var ability) || !HasComp<HumanoidProfileComponent>(args.Target))
            return;

        if (ability.ActiveOnlyWhenFunned && !_fun.CheckRule<HeartRipFunRuleComponent>())
            return;

        if (!HasComp<BodyComponent>(uid)
            || !_body.TryGetOrgansWithComponent<HeartComponent>(uid, out var hearts))
            return;

        var target = args.Target;
        var user = args.User;

        args.Verbs.Add(new InnateVerb
        {
            Text = Loc.GetString("verb-heart-rip-fun"),
            Act = () =>
            {
                _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(ability.Duration),
                        new HeartRipDoAfterEvent(), user, target)
                    {
                        BreakOnMove = true,
                        BreakOnDamage = true,
                        NeedHand = true,
                    });
                _popup.PopupEntity(Loc.GetString("fun-popup-heart-rip-start", ("user", user), ("target", target)), target, PopupType.LargeCaution);
            }
        });
    }

    private void OnDoAfter(EntityUid uid, HeartRipAbilityComponent comp, HeartRipDoAfterEvent args)
    {
        if (args.Cancelled
            || args.Target == null
            || !TryComp<BodyComponent>(args.Target.Value, out var targetBody))
            return;

        if (!_body.TryGetOrgansWithComponent<HeartComponent>(args.Target.Value, out var hearts))
            return;

        var (organUid, _) = hearts.First(); // fuck you bro
        _body.RemoveOrgan((args.Target.Value, targetBody), organUid);
        _hands.TryPickupAnyHand(uid, organUid);
        _stunSystem.TryAddParalyzeDuration(args.Target.Value, comp.ParalyzeTime);
        _audio.PlayPvs(comp.Sound, uid);
        _popup.PopupEntity(Loc.GetString("fun-popup-heart-rip-success", ("user", uid), ("target", args.Target.Value), ("heart", organUid)), uid,  PopupType.LargeCaution);
    }
}
