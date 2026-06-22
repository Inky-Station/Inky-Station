namespace Content.Medical.Server.Inkymed.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class ManualDefibrillatorComponent : Component
{
    [DataField]
    public DefibrillatorChargeSetting? ChargeSetting;
}

public enum DefibrillatorChargeSetting : byte
{// todo inkymed guidebook
    Low,        // 500-1000v    for fibrillations 200-300bpm
    Standard,   // 1200-1500v   for fibrillations 150-250bpm
    High,       // 2000v        for flatline
    Maximum     // 3600v        for fibrillations >50bpm
}
