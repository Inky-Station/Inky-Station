// SPDX-FileCopyrightText: 2026 Impstation contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Inky.Shared.CombatModeVisuals;

/// <summary>
/// Allows the use of unique sprites for combat mode
/// </summary>
[RegisterComponent]
public sealed partial class CombatModeVisualsComponent : Component;

[Serializable, NetSerializable]
public enum CombatModeVisualsVisuals : byte
{
    Combat,
}
