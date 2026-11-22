namespace Content.Shared.Body.Events;

/// <summary>
/// Raised on a brain to set how much oxygen it consumes per breathing cycle.
/// </summary>
[ByRefEvent]
public readonly record struct SetBrainAirConsumptionEvent(float AirConsumption);
