using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.Inkymed;

[NetSerializable, Serializable]
public enum ManualDefibrillatorUiKey : byte
{
    Key
}


[Serializable, NetSerializable]
public sealed class DefibrillatorChargeSettingMessage : BoundUserInterfaceMessage
{
    public readonly DefibrillatorChargeSetting ChargeSetting;

    public DefibrillatorChargeSettingMessage(DefibrillatorChargeSetting chargeSetting)
    {
        ChargeSetting = chargeSetting;
    }
}

[Serializable, NetSerializable]
public sealed class DefibrillatorBuiState : BoundUserInterfaceState
{
    public readonly DefibrillatorChargeSetting ChargeSetting;
    public readonly ProtoId<PulseStatePrototype> PulseState;

    public DefibrillatorBuiState(
        DefibrillatorChargeSetting chargeSetting,
        ProtoId<PulseStatePrototype> pulseState)
    {
        ChargeSetting = chargeSetting;
        PulseState = pulseState;
    }
}
