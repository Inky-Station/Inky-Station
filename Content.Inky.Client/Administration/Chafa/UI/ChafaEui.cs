using Content.Client.Eui;
using Content.Inky.Shared.Administration.Chafa;
using Content.Shared.Eui;

namespace Content.Inky.Client.Administration.Chafa.UI;

public sealed class ChafaEui : BaseEui
{
    private readonly ChafaUi _window;

    public ChafaEui()
    {
        _window = new ChafaUi();
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not ChafaEuiState cast)
            return;

        if (cast.Image is null)
            return;

        _window.SetImage(cast.Image);
    }
}
