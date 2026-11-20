// SPDX-FileCopyrightText: 2026 Impstation contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Impstation.CombatModeVisuals;
using Content.Goobstation.Common.DoAfter;
using Content.Shared.Mobs;
using Robust.Server.GameObjects;

namespace Content.Server._Impstation.CombatModeVisuals;

public sealed partial class CombatModeVisualsSystem : SharedCombatModeVisualsSystem
{
    [Dependency] private AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CombatModeVisualsComponent, CombatModeToggledEvent>(OnCombatToggle);
        SubscribeLocalEvent<CombatModeVisualsComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnCombatToggle(Entity<CombatModeVisualsComponent> ent, ref CombatModeToggledEvent args)
    {
        _appearance.SetData(ent, CombatModeVisualsVisuals.Combat, args.Activated);
    }

    private void OnMobStateChanged(EntityUid ent, CombatModeVisualsComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Critical || args.NewMobState == MobState.Dead)
            _appearance.SetData(ent, CombatModeVisualsVisuals.Combat, false);
    }
}
