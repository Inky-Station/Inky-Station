// SPDX-FileCopyrightText: 2026 Impstation contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Components;
using Robust.Shared.GameStates;

namespace Content.Inky.Shared.CombatModeSprint;

[RegisterComponent, NetworkedComponent]
public sealed partial class CombatModeSprintComponent : Component
{
    [DataField]
    public float SprintCoefficient = 1.5f;

    [DataField]
    public DamageOnHighSpeedImpactComponent? CombatImpact;
}
