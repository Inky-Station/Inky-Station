using Content.Server.Station.Components;
using Content.Server.Station.Systems;

namespace Content.Inky.Server.Goidarmory;

public sealed partial class GoidarmorySystem : EntitySystem
{
    [Dependency] private StationSystem _station = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GoidarmoryComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<GoidarmoryComponent> ent, ref MapInitEvent args)
    {
        var xform = Transform(ent);
        var comp = ent.Comp;
        if (_station.GetStationInMap(xform.MapID) is not { } station
            || !TryComp<StationJobsComponent>(station, out var jobs)
            || !jobs.JobList.TryGetValue(comp.Role,  out var jobAmount))
            return;

        var amount = jobAmount * comp.CountPerRole;
        for (var i = 0; i < amount; i++)
        {
            Spawn(comp.Item, xform.Coordinates);
        }
    }
}
