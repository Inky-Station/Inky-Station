using Robust.Shared.Serialization;

namespace Content.Inky.Shared.Name;

public abstract class SharedSelfRenamerSystem : EntitySystem
{
}

[Serializable, NetSerializable]
public sealed class SelfRenamerNameMessage : BoundUserInterfaceMessage
{
    public readonly string NewName;

    public SelfRenamerNameMessage(string newName)
    {
        NewName = newName;
    }
}
