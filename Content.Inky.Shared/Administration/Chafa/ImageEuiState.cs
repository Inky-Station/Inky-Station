using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Inky.Shared.Administration.Chafa;

[Serializable, NetSerializable]
public sealed class ChafaEuiState(byte[] image) : EuiStateBase
{
    public byte[]? Image = image;
}
