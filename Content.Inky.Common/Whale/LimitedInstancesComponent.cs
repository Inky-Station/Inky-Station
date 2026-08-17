namespace Content.Inky.Common.Whale;

/// <summary>
/// limits how many ents sharing the same key can exist at the same time
/// if the limit exceeds on spawn, the new ent will be deleted.
/// </summary>
[RegisterComponent]
public sealed partial class LimitedInstancesComponent : Component
{
    [DataField(required: true)]
    public string Key = string.Empty;

    [DataField]
    public int Limit = 1;

    [DataField] public bool ServerOnly = true; // in case if some net thing breaks this shit in the future (IDK MAN)
}
