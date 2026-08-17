using Robust.Shared.Serialization;

namespace Content.Inky.Shared.Administration.Chafa;

[Serializable, NetSerializable]
public sealed class ChafaRequestEvent : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class ChafaResponseEvent(byte[] image) : EntityEventArgs
{
    public byte[]? Image = image;
}
