// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Abilities;

/// <summary>
/// Marks the collaborator interface a model's <c>system</c> becomes: one interface per system, whose methods are its
/// abilities. The Screenplay generator reads a command operation that calls these methods back as
/// <c>invokes &lt;System&gt;.&lt;Ability&gt;</c>, taking the system name from the interface name without a leading I.
/// </summary>
public interface ISystemAbilities;
