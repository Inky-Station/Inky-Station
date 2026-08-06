using Content.Shared.Atmos;

namespace Content.Shared._Shitcod; // son

[ByRefEvent]
public record struct SaturateBrainEvent(GasMixture Gas, EntityUid Lung);
