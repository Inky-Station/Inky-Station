namespace Content.Goobstation.Server.MobCaller;

public sealed partial class MobCallerComponent
{
    /// <summary>
    /// if true, will check every entity with SpaceLeviathanComponent before spawning
    /// </summary>
    [DataField]
    public bool IsSpaceWhaleCaller = false; // GOIDA, maybe i should unhardcode it but i cant be bothered lmao
}
