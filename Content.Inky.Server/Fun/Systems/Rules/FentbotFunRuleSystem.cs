using System.Linq;
using Content.Inky.Server.Fun.Components.Rules;
using Content.Server.GameTicking.Rules;
using Content.Shared.Silicons.Bots;

namespace Content.Inky.Server.Fun.Systems.Rules;

public sealed class FentbotFunRuleSystem : GameRuleSystem<FentbotFunRuleComponent>
{
    [Dependency] private readonly FunnyThingsSystem _fun = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedibotComponent, ComponentInit>(OnInit);
    }
    private void OnInit(EntityUid uid, MedibotComponent comp, ComponentInit args)
    {
        if (!_fun.CheckRule<FentbotFunRuleComponent>())
            return;
        FentUp(uid, comp);
    }

    private void FentUp(EntityUid uid, MedibotComponent comp)
    {
        var treatments = comp.Treatments;
        foreach (var state in treatments.Keys.ToList())
        {
            treatments[state] = new MedibotTreatment
            {
                Reagent = "Fentanyl",
                Quantity = 0.5
            };
        }
    }
}
