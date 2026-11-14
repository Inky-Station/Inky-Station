using Content.Inky.Shared.Werewolf.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;

namespace Content.Inky.Shared.Werewolf.Systems;

public sealed partial class SharedWerewolfActionSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SatiationSystem _satiation = default!;

    [SubscribeLocalEvent]
    private void OnActionAttempt(Entity<WerewolfActionComponent> ent, ref ActionAttemptEvent args)
    {
        var user = args.User;
        var comp = ent.Comp;

        if (comp.RequireTransfurmed
            && (!TryComp<WerewolfAbilitiesComponent>(user, out var wolf)
                || !wolf.Transfurmed))
        {
            _popup.PopupClient(Loc.GetString(comp.NotTransfurmedPopup), user, user);
            args.Cancelled = true;
            return;
        }

        if (comp.HungerCost > 0)
        {
            if (!TryComp<SatiationComponent>(user, out var hunger))
                return;

            if (_satiation.GetValueOrNull((user, hunger), SatiationSystem.Hunger) < comp.HungerCost)
            {
                _popup.PopupClient(Loc.GetString(comp.NoHungerPopup), user, user);
                args.Cancelled = true;
                return;
            }
        }

        if (TryComp<SatiationComponent>(user, out var hunger2))
            _satiation.ModifyValue((user, hunger2), SatiationSystem.Hunger, -comp.HungerCost);
    }
}
