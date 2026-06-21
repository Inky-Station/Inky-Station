namespace Content.Shared.Medical;

public sealed partial class DefibrillatorComponent
{
    /// <summary>
    /// amount of BPM being added/reduced on zap
    /// </summary>
    [DataField]
    public int BpmZapHeal = 45;

    /// <summary>
    /// amount of BPM being added/reduced on zap if the heart isnt active
    /// </summary>
    [DataField]
    public int BpmZapHealFlatline = +210;

    /// <summary>
    /// If true, the amount specified will try to go towards the startingheartrate
    /// if the amount specified is negative it will try to de-stabilise it instead
    /// </summary>
    [DataField]
    public bool AutoStabilisation = true;

    [DataField]
    public bool CanDefibAlive = true;
}
