using Robust.Shared.GameStates;

namespace Content.Inky.Shared.Transparent;

/// <summary>
/// replaces the entity sprites opaque pixels with a transparent checker for lolz
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class InkyTransparentComponent : Component;
