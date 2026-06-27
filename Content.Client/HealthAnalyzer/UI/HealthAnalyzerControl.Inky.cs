using Content.Medical.Shared.Body;
using Content.Medical.Shared.Inkymed;
using Content.Shared.Body;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.HealthAnalyzer.UI;

public sealed partial class HealthAnalyzerControl
{
    private BodySystem _bodySystem = default!;
    private HeartRateSystem _heartRateSystem = default!;
    private static readonly ProtoId<OrganCategoryPrototype> HeartCategory = "Heart";

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
}
