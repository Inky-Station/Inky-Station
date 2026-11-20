// SPDX-FileCopyrightText: 2026 Impstation contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Inky.Shared.CombatModeVisuals;
using Content.Goobstation.Common.DoAfter;
using Content.Shared.Mobs;
using Robust.Server.GameObjects;

namespace Content.Inky.Server.CombatModeVisuals;

public sealed partial class CombatModeVisualsSystem : SharedCombatModeVisualsSystem
{
    [Dependency] private AppearanceSystem _appearance = default!;

    [SubscribeLocalEvent]
    private void OnCombatToggle(Entity<CombatModeVisualsComponent> ent, ref CombatModeToggledEvent args)
    {
        _appearance.SetData(ent, CombatModeVisualsVisuals.Combat, args.Activated);
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(EntityUid ent, CombatModeVisualsComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState is MobState.Critical or MobState.Dead)
            _appearance.SetData(ent, CombatModeVisualsVisuals.Combat, false);
    }
}
