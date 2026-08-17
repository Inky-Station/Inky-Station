namespace Content.Inky.Shared.Concussion.Resistance;

[RegisterComponent]
public sealed partial class ConcussionResistanceComponent : Component
{
    [DataField]
    public float Resistance = 0f;
}
