namespace Content.Inky.Common.Medical;

[ByRefEvent]
public struct FindWorkingHeartEvent()
{
    public bool Found = false;

    /// <summary>
    /// Either or not do the special effects if the heart is not found
    /// i.e. bloodloss damage
    /// </summary>
    public bool DoEffects = true; // maybe someone will do something will it too, otherwise woulda called it just DoBloodloss or smth idk
}

public readonly record struct UpdateBloodstreamOverlayEvent();
