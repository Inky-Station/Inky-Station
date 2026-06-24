using Content.Medical.Shared.Inkymed;
using Robust.Server.GameObjects;

namespace Content.Medical.Server.Inkymed.Systems;

public sealed partial class ManualDefibrillatorSystem : EntitySystem
{
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ManualDefibrillatorComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<ManualDefibrillatorComponent, DefibrillatorChargeSettingMessage>(OnSettingChanged);
    }

    private void OnUiOpened(Entity<ManualDefibrillatorComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnSettingChanged(Entity<ManualDefibrillatorComponent> ent, ref DefibrillatorChargeSettingMessage args)
    {
        if ((args.ChargeSetting & DefibrillatorChargeSetting.AllFlips) != args.ChargeSetting)
            return;

        ent.Comp.ChargeSetting = args.ChargeSetting;
        Dirty(ent);
        UpdateUi(ent);
    }

    private void UpdateUi(Entity<ManualDefibrillatorComponent> ent)
    {
        _ui.SetUiState(
            ent.Owner,
            ManualDefibrillatorUiKey.Key,
            new DefibrillatorBuiState(ent.Comp.ChargeSetting, ent.Comp.PulseState));
    }
}
