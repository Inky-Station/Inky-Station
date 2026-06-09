using Content.Inky.Common.Whale;

namespace Content.Inky.Shared;

public sealed class LimitedInstancesSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LimitedInstancesComponent, MapInitEvent>(OnInit);
    }

    private void OnInit(Entity<LimitedInstancesComponent> ent, ref MapInitEvent args)
    {
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
