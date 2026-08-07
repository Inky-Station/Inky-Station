using Content.Shared.Body;
using Content.Shared.Body.Components;
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

public sealed partial class BrainRespiratorThingsThatWillTheBrainRespiratorThingDoSystem : EntitySystem
{
    private static readonly EntProtoId Effect = "StatusEffectOxygenDeprived";
    private static readonly ProtoId<DamageTypePrototype> Asphyxiation = "Asphyxiation";
    private static readonly ProtoId<AlertPrototype> BrainOxygenUnstableAlert = "BrainOxygenUnstable";
    private static readonly ProtoId<AlertPrototype> BrainOxygenDangerousAlert = "BrainOxygenDangerous";
    private static readonly ProtoId<AlertPrototype> BrainOxygenCriticalAlert = "BrainOxygenCritical";
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(4);

    private const float AsphyxiationDamage = 16f; // maybe put it somewhere else idek

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private StatusEffectsSystem _stfx = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private AlertsSystem _alerts = default!;

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
            var target = TryComp<OrganComponent>(uid, out var organ) && organ.Body is { } body
                ? body // fucking kill yourself
                : uid;

            var intensity = GetIntensity(brain.AirSaturation);
            if (intensity == 0f)
            {
                _stfx.TrySetStatusEffectDuration(
                target,
                Effect,
                TimeSpan.FromSeconds(10));
                continue;
            }

            _stfx.TrySetStatusEffectDuration(
                target,
                Effect,
                TimeSpan.FromDays(2) * intensity);

            if (brain.OxygenLevel == BrainOxygen.Critical
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
        var target = TryComp<OrganComponent>(ent.Owner, out var organ) && organ.Body is { } body
            ? body
            : ent.Owner;

        // idek how to make it better
        _alerts.ClearAlert(target, BrainOxygenUnstableAlert);
        _alerts.ClearAlert(target, BrainOxygenDangerousAlert);
        _alerts.ClearAlert(target, BrainOxygenCriticalAlert);

        var alert = args.NewLevel switch
        {
            BrainOxygen.Unstable => BrainOxygenUnstableAlert,
            BrainOxygen.Dangerous => BrainOxygenDangerousAlert,
            BrainOxygen.Critical => BrainOxygenCriticalAlert,
            _ => default(ProtoId<AlertPrototype>?),
        };

        if (alert is { } alertId)
            _alerts.ShowAlert(target, alertId);
    }

}
