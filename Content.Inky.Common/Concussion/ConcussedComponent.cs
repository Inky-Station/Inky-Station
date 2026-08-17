using Robust.Shared.GameStates;

namespace Content.Inky.Common.Concussion;

/// <summary>
/// added while an entity has a <see cref="ConcussionState"/> above .Sane
/// checked by the language system to strip understood languages
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ConcussedComponent : Component;
