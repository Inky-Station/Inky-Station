using System.Linq;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Inkymed;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Medical;
using Robust.Server.GameObjects;

namespace Content.Medical.Server.Inkymed.Systems;

public sealed partial class ManualDefibrillatorSystem : EntitySystem
{
    [Dependency] private ItemToggleSystem _itemToggle = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ManualDefibrillatorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ManualDefibrillatorComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<ManualDefibrillatorComponent, DefibrillatorChargeSettingMessage>(OnSettingChanged);
    }

    private void OnMapInit(Entity<ManualDefibrillatorComponent> ent, ref MapInitEvent args)
    {
        UpdateFibCharge(ent);
    }

    private void OnUiOpened(Entity<ManualDefibrillatorComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnSettingChanged(Entity<ManualDefibrillatorComponent> ent, ref DefibrillatorChargeSettingMessage args)
    {
        if (args.ChargeSetting.Flips.Length != DefibrillatorChargeSetting.FlipAmount)
            return;

        var powered = args.ChargeSetting.Power && args.ChargeSetting.Flips.Any(flip => flip); // KILL YOURSELKF
        if (TryComp<ItemToggleComponent>(ent, out var itemToggle)
            && !_itemToggle.TrySetActive((ent.Owner, itemToggle), powered, args.Actor, predicted: false))
        {
            UpdateUi(ent);
            return;
        }

        ent.Comp.ChargeSetting = args.ChargeSetting.Clone();
        UpdateFibCharge(ent);
        Dirty(ent);
        UpdateUi(ent);
    }

    private void UpdateFibCharge(Entity<ManualDefibrillatorComponent> ent)
    {
        if (!TryComp<DefibrillatorComponent>(ent, out var defibrillator))
            return;

        var flips = ent.Comp.ChargeSetting.Flips.Count(flip => flip);
        defibrillator.BpmZapHeal = ent.Comp.BpmZapFlip // this is a piece of shit
            .Where(threshold => threshold.Key <= flips)
            .MaxBy(threshold => threshold.Key)
            .Value;
        defibrillator.BpmZapHealFlatline = ent.Comp.BpmZapFlatlineFlip
            .Where(threshold => threshold.Key <= flips)
            .MaxBy(threshold => threshold.Key)
            .Value;
        Dirty(ent.Owner, defibrillator);
    }

    public void UpdateUi(Entity<ManualDefibrillatorComponent> ent)
    {
        var bpm = ent.Comp.HeartEntity?.Comp.CurrentRate;

        _ui.SetUiState(
            ent.Owner,
            ManualDefibrillatorUiKey.Key,
            new DefibrillatorBuiState(
                ent.Comp.ChargeSetting,
                ent.Comp.PulseState,
                bpm));
    }
}
