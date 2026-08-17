// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Medical.Shared.Body;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(raiseAfterAutoHandleState: true)] //inkymed
public sealed partial class HeartComponent : Component;
