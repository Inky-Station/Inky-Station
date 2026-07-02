using Content.Inky.Common.Medical;
using Content.Shared.Alert;
using Content.Shared.Body.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Systems;

public sealed partial class BrainSystem
{
    private void InitializeInky()
    {
        SubscribeLocalEvent<AutismComponent, MapInitEvent>((ent, ref args) => UpdateBrainAlert(ent.Owner, brain => brain.AutismAlert, true));
        SubscribeLocalEvent<AutismComponent, ComponentShutdown>((ent, ref args) => UpdateBrainAlert(ent.Owner, brain => brain.AutismAlert, false));

        SubscribeLocalEvent<LobotomisedComponent, MapInitEvent>((ent, ref args) => UpdateBrainAlert(ent.Owner, brain => brain.LobotomyAlert, true));
        SubscribeLocalEvent<LobotomisedComponent, ComponentShutdown>((ent, ref args) => UpdateBrainAlert(ent.Owner, brain => brain.LobotomyAlert, false));
    }
    private void UpdateBrainAlert(
        EntityUid uid,
        Func<BrainComponent, ProtoId<AlertPrototype>?> getAlert, // i am so fucking scared of words
        bool enabled)
    {
        if (!_organQ.TryComp(uid, out var organ)
            || organ.Body is not { } body
            || !_brainQ.TryComp(uid, out var brain)
            || getAlert(brain) is not { } alert)
            return;

        if (enabled)
            _alerts.ShowAlert(body, alert);
        else
            _alerts.ClearAlert(body, alert);
    }
}
