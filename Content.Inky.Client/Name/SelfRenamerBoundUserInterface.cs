using Content.Inky.Shared.Name;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Inky.Client.Name;

[UsedImplicitly]
public sealed class SelfRenamerBoundUserInterface : BoundUserInterface
{
    private SelfRenamerWindow? _window;
    public SelfRenamerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SelfRenamerWindow>();
        _window.OnNameConfirmed += SubmitName;
    }

    private void SubmitName(string name)
    {
        SendMessage(new SelfRenamerNameMessage(name));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Dispose();
    }
}
