using Content.Inky.Common.Medical;

namespace Content.Shared.Body.Systems;

public sealed partial class BrainSystem
{
    private void InitializeInky()
    {
        SubscribeLocalEvent<AutismComponent, MapInitEvent>(OnJoy);
        SubscribeLocalEvent<AutismComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<LobotomisedComponent, MapInitEvent>(OnLobotomised);
        SubscribeLocalEvent<LobotomisedComponent, ComponentShutdown>(OnSs14Deletion);
    }

    private void OnJoy(Entity<AutismComponent> ent, ref MapInitEvent args)
    {
        if (_organQ.TryComp(ent.Owner, out var organ) // holy monoblock
            && organ.Body is { } body
            && _brainQ.TryComp(ent.Owner, out var brain)
            && brain.AutismAlert is { } autismAlert)
            _alerts.ShowAlert(body, autismAlert);
    }

    private void OnShutdown(Entity<AutismComponent> ent, ref ComponentShutdown args)
    {
        if (_organQ.TryComp(ent.Owner, out var organ)
            && organ.Body is { } body
            && _brainQ.TryComp(ent.Owner, out var brain)
            && brain.AutismAlert is { } autismAlert)
            _alerts.ClearAlert(body, autismAlert);
    }

    private void OnLobotomised(Entity<LobotomisedComponent> ent, ref MapInitEvent args)
    {
        if (_organQ.TryComp(ent.Owner, out var organ)
            && organ.Body is { } body
            && _brainQ.TryComp(ent.Owner, out var brain)
            && brain.LobotomyAlert is { } autismAlert)
            _alerts.ShowAlert(body, autismAlert);
    }

    private void OnSs14Deletion(Entity<LobotomisedComponent> ent, ref ComponentShutdown args)
    {
        if (_organQ.TryComp(ent.Owner, out var organ)
            && organ.Body is { } body
            && _brainQ.TryComp(ent.Owner, out var brain)
            && brain.LobotomyAlert is { } autismAlert)
            _alerts.ClearAlert(body, autismAlert);
    }
}
