using Content.Medical.Common.Body;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Inkymed;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Prototypes;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.HealthAnalyzer.UI;

public sealed partial class HealthAnalyzerControl
{
    private BodySystem _bodySystem = default!;
    private HeartRateSystem _heartRateSystem = default!;
    private CommonBodyPartSystem _part = default!;
    private static readonly ProtoId<OrganCategoryPrototype> HeartCategory = "Heart";
    private static readonly ProtoId<OrganCategoryPrototype> BrainCategory = "Brain";
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationDamage = "Asphyxiation";

    private BrainSystem _brainSystem = default!;

    private void PopulateHeartConditions(EntityUid target, string identity)
    {
        var heartUid = _bodySystem.GetOrgan(target, HeartCategory);
        if (heartUid == null
            || !_entityManager.TryGetComponent<HeartComponent>(heartUid, out var heart))
        {
            BpmLabel.Text = Loc.GetString("health-analyzer-window-entity-unknown-value-text");
            return;
        }

        var state = _heartRateSystem.GetState(heart);
        BpmLabel.Text = state != HeartState.Stopped
            ? Loc.GetString("health-analyzer-window-entity-bpm-value-text", ("bpm", MathF.Round(heart.CurrentRate)))
            : Loc.GetString("health-analyzer-window-entity-bpm-stopped-text");

        switch (state)
        {
            case HeartState.Stopped:
                ConditionsListContainer.AddChild(new RichTextLabel
                {
                    Text = Loc.GetString("condition-heart-stopped", ("entity", identity)),
                    Margin = new Thickness(0, 4),
                });
                break;
            case HeartState.Fibrillating:
                ConditionsListContainer.AddChild(new RichTextLabel
                {
                    Text = Loc.GetString("condition-heart-fibrillating", ("entity", identity)),
                    Margin = new Thickness(0, 4),
                });
                break;
        }
    }

    private void PopulateAirSaturationConditions(EntityUid target)
    {
        var uid = _bodySystem.GetOrgan(target, BrainCategory);
        if (uid is { } brain
            && _entityManager.TryGetComponent<BrainComponent>(brain, out var brainComp))
        {
            var oxyLvl = _brainSystem.GetOxygenLevel(brainComp);
            if (oxyLvl is not BrainOxygen.Stable)
                ConditionsListContainer.AddChild(new RichTextLabel
                {
                    Text = Loc.GetString($"condition-brain-oxygen-{oxyLvl}"),
                    Margin = new Thickness(0, 4),
                });
        }

        if (_damageable.GetAllDamage(target).DamageDict.TryGetValue(AsphyxiationDamage, out var asphyxiationDamage)
            && asphyxiationDamage > 75) // i mean
        { // since apstrimyyyy asphyxation and bloodloss are no longer displayed via health analyzer
            ConditionsListContainer.AddChild(new RichTextLabel
            {
                Text = Loc.GetString("condition-body-asphyxiation-severe"),
                Margin = new Thickness(0, 4),
            });
        }
    }
}
