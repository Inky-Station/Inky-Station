using Content.Inky.Common.Events.Werewolf;

namespace Content.Trauma.Shared.Knowledge.Systems;

public abstract partial class SharedKnowledgeSystem
{
    private void InitializeInky()
    {
        SubscribeLocalEvent<SelectFirstMartialArtEvent>(OnSelectFirstMartialArt);
    }
    /// <summary>
    /// selects the first martial art from the known martial arts
    /// </summary>
    private void OnSelectFirstMartialArt(SelectFirstMartialArtEvent args)
    {
        if (GetContainer(args.Entity) is not { } container
            || container.Comp.ActiveMartialArt != null)
            return;

        foreach (var knowledgeUid in container.Comp.KnowledgeDict.Values)
        {
            if (!_artQuery.HasComp(knowledgeUid)
                || !Exists(knowledgeUid))
                continue;

            ChangeMartialArts(container, args.Entity, knowledgeUid);
            return;
        }
    }
}
