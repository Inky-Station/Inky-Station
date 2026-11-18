// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;

namespace Content.Inky.Shared.Entry;

public sealed partial class EntryPoint : GameShared // todo inky eeenkeee DOESNT WORK
{
    [Dependency] private IPrototypeManager _proto = default!;

    public override void PreInit()
    {
        Dependencies.InjectDependencies(this);
    }

    public override void Init()
    {
        _proto.PartialDirectory(new("/Prototypes/_Trauma/Partials"), 1);
    }
}
