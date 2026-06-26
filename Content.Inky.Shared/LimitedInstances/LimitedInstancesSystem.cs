using Content.Inky.Common.Whale;
using Robust.Shared.Network;

namespace Content.Inky.Shared.LimitedInstances;

public sealed partial class LimitedInstancesSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LimitedInstancesComponent, MapInitEvent>(OnInit);
    }

    private void OnInit(Entity<LimitedInstancesComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.ServerOnly && _net.IsClient)
            return;

        var count = 0;
        var eqe = EntityQueryEnumerator<LimitedInstancesComponent>();
        while (eqe.MoveNext(out var uid, out var other))
        {
            if (uid == ent.Owner || other.Key != ent.Comp.Key)
                continue;

            count++;
        }

        if (count >= ent.Comp.Limit)
        {
            Log.Info($"Entity {ToPrettyString(ent.Owner)} exceeded the limit on LimitedInstancesComponent, deleting..."); // to avoid confussion in the future
            QueueDel(ent.Owner);
        }
    }
}
