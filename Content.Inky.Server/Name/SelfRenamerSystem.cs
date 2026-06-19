using Content.Inky.Shared.Name;
using Content.Server.Actions;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Inky.Server.Name;

public sealed class SelfRenamerSystem : SharedSelfRenamerSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SelfRenamerComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<SelfRenamerComponent, SelfRenamerActionEvent>(OnAction);

        Subs.BuiEvents<SelfRenamerComponent>(SelfRenamerUIKey.Key, subs => { subs.Event<SelfRenamerNameMessage>(OnNameMessage); });
    }
    private void OnInit(Entity<SelfRenamerComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.ActionPrototype);
    }

    private void OnAction(Entity<SelfRenamerComponent> ent, ref SelfRenamerActionEvent args)
    {
        if (!TryComp<ActorComponent>(ent, out var actor))
            return;

        _ui.OpenUi(ent.Owner, SelfRenamerUIKey.Key, actor.PlayerSession);
        args.Handled = true;
    }

    private void OnNameMessage(Entity<SelfRenamerComponent> ent, ref SelfRenamerNameMessage args)
    {
        var newName = args.NewName.Trim();

        if (newName.Length == 0)
            return;

        if (newName.Length > 31)
            newName = newName[..31];

        _meta.SetEntityName(ent, newName);
        _ui.CloseUi(ent.Owner, SelfRenamerUIKey.Key);
    }
}
