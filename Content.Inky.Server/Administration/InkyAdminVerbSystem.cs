using Content.Server.Administration.Managers;
using Content.Shared.Body;
using Robust.Shared.Player;

namespace Content.Inky.Server.Administration;

/// <summary>
/// This is used for...
/// </summary>
public sealed partial class InkyAdminVerbSystem : EntitySystem
{
    [Dependency] private IAdminManager _admin = default!;
    [Dependency] private BodySystem _body = default!;

    [Dependency] private EntityQuery<ActorComponent> _actors = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        InitializeSmites();
    }
}
