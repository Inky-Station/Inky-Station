namespace Content.Shared.Blocking.Components;

public sealed partial class BlockingUserComponent
{
    [DataField, AutoNetworkedField] // inky edit
    public float MovementModifier = 1f;
}
