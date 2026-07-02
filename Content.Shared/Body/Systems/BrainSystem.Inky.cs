using Content.Inky.Common.Medical;
using Content.Shared.Alert;
using Content.Shared.Body.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Systems;

public sealed partial class BrainSystem
{
    private void InitializeInky()
    {
        SubscribeLocalEvent<AutismComponent, MapInitEvent>(OnAutism);
        SubscribeLocalEvent<AutismComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<LobotomisedComponent, MapInitEvent>(OnLobotomised);
        SubscribeLocalEvent<LobotomisedComponent, ComponentShutdown>(OnSs14Deletion);
    }

    private void OnAutism(Entity<AutismComponent> ent, ref MapInitEvent args) => UpdateBrainAlert(ent.Owner, brain => brain.AutismAlert, true);

    private void OnShutdown(Entity<AutismComponent> ent, ref ComponentShutdown args) => UpdateBrainAlert(ent.Owner, brain => brain.AutismAlert, false);

    private void OnLobotomised(Entity<LobotomisedComponent> ent, ref MapInitEvent args) => UpdateBrainAlert(ent.Owner, brain => brain.LobotomyAlert, true);

    private void OnSs14Deletion(Entity<LobotomisedComponent> ent, ref ComponentShutdown args) => UpdateBrainAlert(ent.Owner, brain => brain.LobotomyAlert, false);

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
