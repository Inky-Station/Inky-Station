using Content.Medical.Shared.Body;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.HealthAnalyzer.UI;

public sealed partial class HealthAnalyzerControl
{
    private void PopulateHeartConditions(EntityUid target, string identity)
    {
        var heartUid = _bodySystem.GetOrgan(target, HeartCategory);
        if (heartUid == null
            || !_entityManager.TryGetComponent<HeartComponent>(heartUid, out var heart))
        {
            BpmLabel.Text = Loc.GetString("health-analyzer-window-entity-unknown-value-text");
            return;
        }

        BpmLabel.Text = heart.CurrentlyActive
            ? Loc.GetString("health-analyzer-window-entity-bpm-value-text", ("bpm", heart.CurrentHeartRate))
            : Loc.GetString("health-analyzer-window-entity-bpm-stopped-text");

        if (!heart.CurrentlyActive)
        {
            ConditionsListContainer.AddChild(new RichTextLabel
            {
                Text = Loc.GetString("condition-heart-stopped", ("entity", identity)),
                Margin = new Thickness(0, 4),
            });
            return; // stopped implies not fibrillating
        }

        if (heart.CurrentlyFibrillating)
        {
            ConditionsListContainer.AddChild(new RichTextLabel
            {
                Text = Loc.GetString("condition-heart-fibrillating", ("entity", identity)),
                Margin = new Thickness(0, 4),
            });
        }
    }

}
