using Content.Shared.Roles;
using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.Prototypes;

namespace Content.Inky.Shared.Jobs;

public sealed partial class GiveKnowledgeSpecial : JobSpecial
{
    [DataField(required: true)]
    public Dictionary<EntProtoId, int> Skills = new();

    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>(); // fuck you
        entMan.System<SharedKnowledgeSystem>().AddKnowledgeUnits(mob, Skills);
    }
}
