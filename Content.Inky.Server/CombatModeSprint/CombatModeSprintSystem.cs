// SPDX-FileCopyrightText: 2026 Impstation contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.DoAfter;
using Content.Inky.Shared.CombatModeSprint;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;

namespace Content.Inky.Server.CombatModeSprint;

public sealed partial class CombatModeSprintSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private SharedCombatModeSystem _combatMode = default!;
    [Dependency] private DamageOnHighSpeedImpactSystem _impact = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnToggleCombat(EntityUid uid, CombatModeSprintComponent component, ref CombatModeToggledEvent args)
    {
        if (args.Activated &&
            component.CombatImpact is {} impact)
        {
            _impact.ChangeCollide(uid,
                impact.MinimumSpeed,
                impact.StunSeconds,
                impact.DamageCooldown,
                impact.SpeedDamageFactor);
        }

        _movementSpeed.RefreshMovementSpeedModifiers(uid);

        if (!args.Activated)
            return;

        var message = Loc.GetString("begin-combat-popup", ("name", MetaData(uid).EntityName));
        _popup.PopupEntity(message, uid, uid);
    }

    [SubscribeLocalEvent]
    private void OnRefreshMovespeed(EntityUid uid, CombatModeSprintComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (_combatMode.IsInCombatMode(uid))
            args.ModifySpeed(component.SprintCoefficient);
    }
}
