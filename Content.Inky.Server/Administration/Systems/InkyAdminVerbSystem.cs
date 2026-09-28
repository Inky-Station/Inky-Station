using Content.Server.Administration.Systems;
using Content.Shared.Verbs;

namespace Content.Inky.Server.Administration.Systems;

public sealed partial class InkyAdminVerbSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void GetVerbs(GetVerbsEvent<Verb> ev)
    {
        AddAdminVerbs(ev);
    }
}
