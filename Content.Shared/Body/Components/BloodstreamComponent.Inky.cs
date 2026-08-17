namespace Content.Shared.Body.Components;

public sealed partial class BloodstreamComponent
{
    /// <summary>
    /// If true, would require at least one working heart in the owner, or would consider them having no blood.
    /// </summary>
    [DataField]
    public bool RequiresHeart = true; // GOIDA
}
