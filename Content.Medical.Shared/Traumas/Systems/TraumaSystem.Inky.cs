using Content.Medical.Common.Body;
using Content.Shared.Rejuvenate;

namespace Content.Medical.Shared.Traumas;

public sealed partial class TraumaSystem
{
    private void InitInky()
    {
        SubscribeLocalEvent<InternalOrganComponent, RejuvenateEvent>((ent, ref ev) => ent.Comp.OrganIntegrity = ent.Comp.IntegrityCap);
    }
}
