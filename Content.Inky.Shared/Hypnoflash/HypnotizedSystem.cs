using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.Roles;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Inky.Shared.Hypnoflash;

public sealed partial class HypnotizedSystem : EntitySystem
{
    private static readonly EntProtoId MutedEffect = "StatusEffectMuted";

    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedObjectivesSystem _objectives = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private SharedStunSystem _stunSystem = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HypnotizedComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<HypnotizedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<HypnotizedComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<HypnotizedConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
        SubscribeLocalEvent<MindContainerComponent, HypnoflashedEvent>(OnHypnotized);
    }

    private void OnInit(Entity<HypnotizedComponent> ent, ref ComponentInit args)
    {
        _statusEffects.TrySetStatusEffectDuration(ent, MutedEffect); // so you dont hypnotize yourself by mistake
        EnsureComp<ActiveListenerComponent>(ent);

        ent.Comp.EndTime = _timing.CurTime + ent.Comp.Duration;
        _stunSystem.TryKnockdown(ent.Owner, TimeSpan.FromSeconds(4));
    }

    public override void Update(float frameTime) // so you dont stay muted forever idk
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HypnotizedComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime >= comp.EndTime)
                RemCompDeferred<HypnotizedComponent>(uid);
        }
    }
    private void OnListen(Entity<HypnotizedComponent> ent, ref ListenEvent args)
    {
        var message = args.Message.Trim();

        if (string.IsNullOrWhiteSpace(message))
            return;

        if (!_mind.TryGetMind(ent, out var mindId, out var mind))
            return;

        var objectiveId = Spawn("HypnotizedObjective"); // goidaExpand commentComment on line R63Resolved
        _meta.SetEntityDescription(objectiveId, message);
        _mind.AddObjective(mindId, mind, objectiveId);

        _statusEffects.TryRemoveStatusEffect(ent, MutedEffect); // when the mimes talk...
        RemCompDeferred<HypnotizedComponent>(ent);
        RemCompDeferred<ActiveListenerComponent>(ent);
        _stunSystem.TryKnockdown(ent.Owner, TimeSpan.FromSeconds(4));
    }

    private void OnShutdown(Entity<HypnotizedComponent> ent, ref ComponentShutdown args)
    {
        RemCompDeferred<ActiveListenerComponent>(ent);
        _statusEffects.TryRemoveStatusEffect(ent, MutedEffect);
    }

    private void OnGetProgress(EntityUid uid, HypnotizedConditionComponent comp, ref ObjectiveGetProgressEvent args)
        => args.Progress = 0f; // "Objective X(xx/nxx) of john goida (xx/nxx) didnt set a progress value!" error my ass

    private void OnHypnotized(Entity<MindContainerComponent> ent, ref HypnoflashedEvent args)
    {
        EnsureComp<HypnotizedComponent>(ent.Owner);
        if (_mind.TryGetMind(ent.Owner, out var mindId, out var mind))
            _role.MindAddRole(mindId, "MindRoleHypnotized"); // free agent status, but still must follow his objectives right? change to familiar if shitters be shitters
    }
}
