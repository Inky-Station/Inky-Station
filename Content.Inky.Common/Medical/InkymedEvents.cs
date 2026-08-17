namespace Content.Inky.Common.Medical;

[ByRefEvent]
public struct FindWorkingHeartEvent()
{
    public bool Found = false;
}

public readonly record struct UpdateBloodstreamOverlayEvent();
