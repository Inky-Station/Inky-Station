namespace Content.Shared.Body;

// the code is perfect.
public abstract partial class SharedVisualBodySystem
{
    protected virtual void SetOrganSecondColor(Entity<VisualOrganComponent> ent, Color color)
    {
        if (ent.Comp.SecondData == null)
            return;
        ent.Comp.SecondData.Color = color;
        Dirty(ent);
    }

    protected virtual void SetOrganSecondAppearance(Entity<VisualOrganComponent> ent, PrototypeLayerData data)
    {
        ent.Comp.SecondData = data;
        Dirty(ent);
    }
}
