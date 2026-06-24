using System.Numerics;
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
        UpdateFibCharge(ent.Owner, ent.Comp.ChargeSetting);
    }

    private void OnUiOpened(Entity<ManualDefibrillatorComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnSettingChanged(Entity<ManualDefibrillatorComponent> ent, ref DefibrillatorChargeSettingMessage args)
    {
        if ((args.ChargeSetting & DefibrillatorChargeSetting.AllFlips) != args.ChargeSetting)
            return;

        var voltage = args.ChargeSetting & DefibrillatorChargeSetting.AllMinusPower;
        var powered = args.ChargeSetting.HasFlag(DefibrillatorChargeSetting.PowerFlip)
                      && BitOperations.PopCount((uint) voltage) > 0; // https://discord.com/channels/1491179642655215736/1491180108449448107/1519444544750354563
        if (TryComp<ItemToggleComponent>(ent, out var itemToggle)
            && !_itemToggle.TrySetActive((ent.Owner, itemToggle), powered, args.Actor, predicted: false))
        {
            UpdateUi(ent);
            return;
        }

        ent.Comp.ChargeSetting = args.ChargeSetting;
        UpdateFibCharge(ent.Owner, args.ChargeSetting);
        Dirty(ent);
        UpdateUi(ent);
    }

    private void UpdateFibCharge(EntityUid uid, DefibrillatorChargeSetting setting)
    {
        if (!TryComp<DefibrillatorComponent>(uid, out var defibrillator))
            return;

        var voltage = setting & DefibrillatorChargeSetting.AllMinusPower;
        var flips = BitOperations.PopCount((uint) voltage);

        (defibrillator.BpmZapHeal, defibrillator.BpmZapHealFlatline) = flips switch
        {
            0 => (0, 0),     // one flip - 500v - if bpm is 200+, lowers it by 80
            1 => (-80, 0),    // two flips - 1000v - if bpm is 110-200, lowers it by 50
            2 => (-50, 0),    // three flips - 1500v - if bpm is 0, raises it by 200
            3 => (150, 200), // four flips - 2000v - if bpm is lower than 50
            4 => (200, 300),
            _ => (0, 0),
        };

        Dirty(uid, defibrillator);
    }

    public void UpdateUi(Entity<ManualDefibrillatorComponent> ent)
    {
        _ui.SetUiState(
            ent.Owner,
            ManualDefibrillatorUiKey.Key,
            new DefibrillatorBuiState(
                ent.Comp.ChargeSetting,
                ent.Comp.PulseState,
                ent.Comp.TargetEntity == null ? null : ent.Comp.Bpm));
    }
}
