using Robust.Shared.Serialization;
namespace Content.Inky.Common.Whale;

[Serializable, NetSerializable]
public sealed class LeviathanMusicStartEvent : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class LeviathanMusicStopEvent : EntityEventArgs;
