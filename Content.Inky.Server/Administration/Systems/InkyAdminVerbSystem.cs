using Content.Server.Administration.Managers;
using Content.Shared.Verbs;

namespace Content.Inky.Server.Administration.Systems;

public sealed partial class InkyAdminVerbSystem : EntitySystem
{
    [Dependency] private IAdminManager _adminManager = default!;

    [SubscribeLocalEvent]
    private void GetVerbs(GetVerbsEvent<Verb> ev)
    {
        AddAdminVerbs(ev);
    }
}
