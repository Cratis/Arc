// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// The exception that is thrown when a lookup declaration names no command property or targets no read model.
/// </summary>
/// <param name="commandType">The command type.</param>
/// <param name="property">The declared command property.</param>
public class InvalidLookUpBy(Type commandType, string property) : Exception($"LookUpBy('{property}') on command '{commandType}' requires a read-model parameter and an existing command property.")
{
    readonly string? _reason;

    /// <summary>
    /// Initializes a refusal for a lookup that cannot select this model's key.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <param name="property">The declared command property.</param>
    /// <param name="reason">Why this lookup is not supported.</param>
    public InvalidLookUpBy(Type commandType, string property, string reason)
        : this(commandType, property) => _reason = reason;

    /// <inheritdoc/>
    public override string Message => _reason is null ? base.Message : $"LookUpBy('{property}') on command '{commandType}' is invalid: {_reason}.";
}
