namespace Content.Shared.Blocking;

public sealed partial class BlockingComponent
{
    /// <summary>
    /// The movement modifier that is applied when an entity raises the shield
    /// </summary>
    [DataField]
    public float MovementModifier = 0.2f;
}
