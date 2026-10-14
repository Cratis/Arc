// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Declares the command key guard rollout mode.
/// </summary>
/// <param name="mode">The declared mode.</param>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class KeyGuardAttribute(KeyGuardMode mode) : Attribute, IKeyGuardAttribute
{
    /// <summary>
    /// Gets the declared mode.
    /// </summary>
    public KeyGuardMode Mode { get; } = mode;
}
