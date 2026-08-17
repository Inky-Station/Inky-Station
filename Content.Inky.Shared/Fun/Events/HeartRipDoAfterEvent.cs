using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Inky.Shared.Fun.Events;

[Serializable, NetSerializable]
public sealed partial class HeartRipDoAfterEvent : SimpleDoAfterEvent {}
