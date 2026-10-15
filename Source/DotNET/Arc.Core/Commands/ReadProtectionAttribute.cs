// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Declares the read-protection rollout mode for a command or its assembly.
/// </summary>
/// <param name="mode">The declared mode.</param>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class ReadProtectionAttribute(ReadProtectionMode mode) : Attribute
{
    /// <summary>
    /// Gets the declared mode.
    /// </summary>
    public ReadProtectionMode Mode { get; } = mode;
}
