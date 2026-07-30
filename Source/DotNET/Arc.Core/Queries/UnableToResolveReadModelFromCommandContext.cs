// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Queries;

/// <summary>
/// Exception that gets thrown when not being able to resolve a read model from command context.
/// </summary>
/// <param name="readModelType">Type of read model that could not be resolved.</param>
/// <remarks>
/// A read model is keyed by the command's resolved key, so a command that carries no usable identifier can never resolve
/// one — that is invalid client input, not a server fault. This implements <see cref="IValidationFailure"/> so the
/// command pipeline surfaces it as a validation failure (HTTP 400). The detailed message (naming the read model type) is
/// for server logs only; the client sees the generic validation message.
/// </remarks>
public class UnableToResolveReadModelFromCommandContext(Type readModelType)
    : Exception($"Unable to resolve read model of type '{readModelType.FullName}' from command context. Make sure the command declares its key: with Chronicle, a property assignable to EventSourceId or carrying Cratis.Chronicle.Keys.KeyAttribute; without it, a property carrying System.ComponentModel.DataAnnotations.KeyAttribute, or an ICanProvideKeyForCommand implementation"),
      IValidationFailure
{
    /// <summary>
    /// Gets the read model type that could not be resolved.
    /// </summary>
    public Type ReadModelType { get; } = readModelType;

    /// <inheritdoc/>
    public ValidationResult ValidationResult { get; } = ValidationResult.Error("The command is missing the identifier required to load its current state.");
}
