// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Declares the preferred command read path.
/// </summary>
/// <param name="consistency">The declared consistency.</param>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class ConsistencyAttribute(ReadConsistency consistency) : Attribute
{
    /// <summary>
    /// Gets the declared consistency.
    /// </summary>
    public ReadConsistency Consistency { get; } = consistency;
}
