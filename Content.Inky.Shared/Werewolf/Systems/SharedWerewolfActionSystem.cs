using Content.Inky.Shared.Werewolf.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;

namespace Content.Inky.Shared.Werewolf.Systems;

public sealed partial class SharedWerewolfActionSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private HungerSystem _hunger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WerewolfActionComponent, ActionAttemptEvent>(OnActionAttempt);
    }

    private void OnActionAttempt(Entity<WerewolfActionComponent> ent, ref ActionAttemptEvent args)
    {
        var user = args.User;
        var comp = ent.Comp;

        if (comp.RequireTransfurmed)
        {
            if (!TryComp<WerewolfAbilitiesComponent>(user, out var wolf) || !wolf.Transfurmed)
            {
                _popup.PopupClient(Loc.GetString(comp.NotTransfurmedPopup), user, user);
                args.Cancelled = true;
                return;
            }
        }

        if (comp.HungerCost > 0)
        {
            if (!TryComp<HungerComponent>(user, out var hunger))
                return;

            if (_hunger.GetHunger(hunger) < comp.HungerCost)
            {
                _popup.PopupClient(Loc.GetString(comp.NoHungerPopup), user, user);
                args.Cancelled = true;
                return;
            }
        }

        _hunger.ModifyHunger(user, -comp.HungerCost);
    }
}
