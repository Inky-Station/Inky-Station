// SPDX-FileCopyrightText: 2026 Impstation contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.DoAfter;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Shared._Impstation.CombatModeSprint;

public abstract class SharedCombatModeSprintSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private SharedCombatModeSystem _combatMode = default!;
    [Dependency] private DamageOnHighSpeedImpactSystem _impact = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CombatModeSprintComponent, CombatModeToggledEvent>(OnToggleCombat);
        SubscribeLocalEvent<CombatModeSprintComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
    }

    private void OnToggleCombat(Entity<CombatModeSprintComponent> ent, ref CombatModeToggledEvent args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(ent.Owner);
        if (ent.Comp.BeginCombatMessage != null && _combatMode.IsInCombatMode(ent))
            _popup.PopupEntity(Loc.GetString(ent.Comp.BeginCombatMessage, ("name", Identity.Entity(ent, EntityManager))), ent, Filter.PvsExcept(ent), true);
        if (ent.Comp.EndCombatMessage != null && !_combatMode.IsInCombatMode(ent))
            _popup.PopupEntity(Loc.GetString(ent.Comp.EndCombatMessage, ("name", Identity.Entity(ent, EntityManager))), ent, Filter.PvsExcept(ent), true);
    }

    private void OnRefreshMovespeed(EntityUid ent, CombatModeSprintComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (_combatMode.IsInCombatMode(ent))
        {
            args.ModifySpeed(component.SprintCoefficient);
            if (component.DoImpactDamage)
                _impact.ChangeCollide(ent, component.MinimumSpeed, component.StunSeconds, component.DamageCooldown, component.SpeedDamage);
        }
        else
        {
            args.ModifySpeed(1f);
            if (component.DoImpactDamage)
                _impact.ChangeCollide(ent, component.DefaultMinimumSpeed, component.DefaultStunSeconds, component.DefaultDamageCooldown, component.DefaultSpeedDamage);
        }
    }
}
