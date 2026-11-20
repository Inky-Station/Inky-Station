// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Server.SpaceWhale;

public sealed partial class SpaceWhaleTargetComponent
{
    [DataField]
    public TimeSpan DespawnDelay = TimeSpan.FromSeconds(20);
}
