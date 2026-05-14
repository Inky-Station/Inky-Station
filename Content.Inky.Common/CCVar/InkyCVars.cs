using Robust.Shared.Configuration;

namespace Content.Inky.Common.CCVar;

[CVarDefs]
public sealed partial class InkyCVars
{
    /// <summary>
    /// % chance for anything in <see cref="FunnyThingsSystem"/> to roll, 1f is 100%
    /// </summary>
    public static readonly CVarDef<float> FunProb =
        CVarDef.Create("inky.fun_value", 0.1f, CVar.SERVER); // 10%

    public static readonly CVarDef<bool> ConcussionSound =
        CVarDef.Create("inky.concussion_sound", true, CVar.CLIENTONLY | CVar.ARCHIVE);
}
