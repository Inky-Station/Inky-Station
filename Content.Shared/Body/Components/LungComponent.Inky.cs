namespace Content.Shared.Body.Components;

public sealed partial class LungComponent
{
    [DataField, AutoNetworkedField]
    public float AirSaturationGain = 0.05f;
}
