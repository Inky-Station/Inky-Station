namespace Content.Shared.Body;

public sealed partial class VisualOrganComponent // this is fucking horrible but i cant be bothered to unfuck it
{
    [DataField, AutoNetworkedField]
    public Enum? SecondLayer;

    [DataField, AutoNetworkedField, AlwaysPushInheritance]
    public PrototypeLayerData? SecondData;
}
