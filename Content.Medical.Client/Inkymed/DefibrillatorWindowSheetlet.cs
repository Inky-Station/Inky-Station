using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.UserInterface.CustomControls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Medical.Client.Inkymed;

[CommonSheetlet]
public sealed partial class DefibSheetlet : Sheetlet<NanotrasenStylesheet>
{
    public override StyleRule[] GetRules(NanotrasenStylesheet sheet, object config)
    {
        var transparent = new StyleBoxFlat
        {
            BackgroundColor = Color.Transparent,
        };

        return
        [
            E()
                .Class("DefibWindow")
                .ParentOf(E().Class(DefaultWindow.StyleClassWindowPanel))
                .Panel(transparent),
        ];
    }
}
