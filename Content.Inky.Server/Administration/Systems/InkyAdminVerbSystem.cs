using Content.Server.Administration.Systems;

namespace Content.Inky.Server.Administration.Systems;

public sealed partial class InkyAdminVerbSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetAntagVerbsEvent>(OnGetAntagVerbs);
    }
}
