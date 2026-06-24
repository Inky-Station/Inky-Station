using Content.Medical.Shared.Inkymed;
using Robust.Client.GameObjects;

namespace Content.Medical.Client.Inkymed;

public sealed class ManualDefibrillatorBui : BoundUserInterface
{
    private DefibrillatorWindow? _window;

    public ManualDefibrillatorBui(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();
        _window = new DefibrillatorWindow();
        _window.OnClose += OnWindowClosed;
        _window.OnChargeSettingSelected += OnSettingSelected;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not DefibrillatorBuiState defibState || _window == null)
            return;

        _window.SetActiveSetting(defibState.ChargeSetting);
        _window.SetPulseState(defibState.PulseState);
        _window.SetBpm(defibState.Bpm);
    }

    private void OnSettingSelected(DefibrillatorChargeSetting setting)
    {
        SendMessage(new DefibrillatorChargeSettingMessage(setting));
    }

    private void OnWindowClosed()
    {
        _window?.FlushPendingSetting();
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Close();
    }
}
