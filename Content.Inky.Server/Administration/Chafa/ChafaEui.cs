using Content.Server.EUI;
using Content.Inky.Shared.Administration.Chafa;

namespace Content.Inky.Server.Administration.Chafa;

public sealed class ChafaEui(byte[] image) : BaseEui
{
    private readonly byte[] _image = image;

    public override ChafaEuiState GetNewState()
    {
        return new ChafaEuiState(_image);
    }
}
