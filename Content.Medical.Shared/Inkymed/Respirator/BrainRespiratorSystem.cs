using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Medical.Common.Targeting;
using Content.Shared.Alert;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Medical.Shared.Inkymed.Respirator;

public sealed partial class BrainRespiratorSystem : EntitySystem
{
    private static readonly EntProtoId Effect = "StatusEffectOxygenDeprived";
    private static readonly ProtoId<DamageTypePrototype> Asphyxiation = "Asphyxiation";
    private static readonly ProtoId<AlertPrototype> BrainOxygenUnstableAlert = "BrainOxygenUnstable";
    private static readonly ProtoId<AlertPrototype> BrainOxygenDangerousAlert = "BrainOxygenDangerous";
    private static readonly ProtoId<AlertPrototype> BrainOxygenCriticalAlert = "BrainOxygenCritical";
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(4);

    private const float AsphyxiationDamage = 16f; // maybe put it somewhere else idek

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private BrainSystem _brain = default!;
    [Dependency] private EntityQuery<OrganComponent> _organQ = default!;

    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();
        _nextUpdate = _timing.CurTime + UpdateInterval;
        SubscribeLocalEvent<BrainComponent, BrainOxygenLevelChangedEvent>(OnBrainOxygenLevelChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        var eqe = EntityQueryEnumerator<BrainComponent>();
        while (eqe.MoveNext(out var uid, out var brain))
        {
            _organQ.TryComp(uid, out var organ);
            var target = organ?.Body ?? uid;

            var intensity = GetIntensity(brain.AirSaturation);
            if (intensity == 0f)
            {
                _statusEffects.TrySetStatusEffectDuration(
                target,
                Effect,
                TimeSpan.FromSeconds(10));
                continue;
            }

            _statusEffects.TrySetStatusEffectDuration(
                target,
                Effect,
                TimeSpan.FromDays(2) * intensity);

            if (_brain.GetOxygenLevel(brain) == BrainOxygen.Critical
                && !_mobState.IsDead(target))
            {
                _damageable.ChangeDamage(
                    target,
                    new DamageSpecifier(ProtoMan.Index(Asphyxiation), AsphyxiationDamage),
                    targetPart: TargetBodyPart.Vital,
                    interruptsDoAfters: false,
                    ignoreResistances: true);
            }
        }
    }

    private static float GetIntensity(float saturation)
    {
        return Math.Clamp((0.9f - saturation) / 0.55f * 0.65f, 0f, 0.65f); // im ngl i was putting random ass numbers here from BrainSystem.GetOxygenLevel
    }

    private void OnBrainOxygenLevelChanged(Entity<BrainComponent> ent, ref BrainOxygenLevelChangedEvent args)
    {
        _organQ.TryComp(ent.Owner, out var organ);
        var target = organ?.Body ?? ent.Owner;

        _alerts.ClearAlert(target, BrainOxygenUnstableAlert);
        _alerts.ClearAlert(target, BrainOxygenDangerousAlert);
        _alerts.ClearAlert(target, BrainOxygenCriticalAlert);

        var alert = args.NewLevel switch
        {
            BrainOxygen.Unstable => BrainOxygenUnstableAlert,
            BrainOxygen.Dangerous => BrainOxygenDangerousAlert,
            BrainOxygen.Critical => BrainOxygenCriticalAlert,
            _ => default(ProtoId<AlertPrototype>?)
        };

        if (alert is { } alertId)
            _alerts.ShowAlert(target, alertId);
    }

}
