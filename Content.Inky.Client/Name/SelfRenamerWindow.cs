using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Inky.Client.Name;

public sealed partial class SelfRenamerWindow : FancyWindow
{
    private const int MaxNameLength = 31;

    public event Action<string>? OnNameConfirmed;

    private readonly LineEdit _nameEdit;
    private readonly Button _confirmButton;

    public SelfRenamerWindow()
    {
        RobustXamlLoader.Load(this);

        _nameEdit = FindControl<LineEdit>("NameEdit");
        _confirmButton = FindControl<Button>("ConfirmButton");

        _nameEdit.OnTextChanged += OnTextChanged;
        _nameEdit.OnTextEntered += _ => TryConfirm();
        _confirmButton.OnPressed += _ => TryConfirm();

        _confirmButton.Disabled = true;
    }

    private void OnTextChanged(LineEdit.LineEditEventArgs args)
    {
        if (_nameEdit.Text.Length > MaxNameLength)
            _nameEdit.Text = _nameEdit.Text[..MaxNameLength];

        _confirmButton.Disabled = _nameEdit.Text.Trim().Length == 0;
    }

    private void TryConfirm()
    {
        var name = _nameEdit.Text.Trim();
        if (name.Length == 0)
            return;

        OnNameConfirmed?.Invoke(name);
    }
}
