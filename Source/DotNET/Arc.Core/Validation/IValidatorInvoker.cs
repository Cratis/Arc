// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation;

/// <summary>
/// Defines a system that runs a validator against an instance and expresses the outcome as framework
/// <see cref="ValidationResult"/> instances.
/// </summary>
/// <remarks>
/// This is the only place that knows the validation library's shape — how a validation context is built, how its
/// severities map, and what happens when a validator throws. Keeping it apart from the traversal means the graph
/// walk deals purely in "what should be validated", and swapping or extending the validation library touches one type.
/// </remarks>
public interface IValidatorInvoker
{
    /// <summary>
    /// Runs a validator against an instance.
    /// </summary>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="validator">The <see cref="IValidator"/> to run.</param>
    /// <param name="path">The camelCased member path from the graph root to <paramref name="instance"/>, empty at the root.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/> for cancelling the validation.</param>
    /// <returns>The <see cref="ValidationResult"/> collection describing the outcome, empty when valid.</returns>
    Task<IEnumerable<ValidationResult>> Invoke(object instance, IValidator validator, string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a validator against an instance, telling the validator what it is being run for.
    /// </summary>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="validator">The <see cref="IValidator"/> to run.</param>
    /// <param name="path">The camelCased member path from the graph root to <paramref name="instance"/>, empty at the root.</param>
    /// <param name="kind">What the instance is validated for, or null when not known.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for cancelling the validation.</param>
    /// <returns>The <see cref="ValidationResult"/> collection describing the outcome, empty when valid.</returns>
    /// <remarks>
    /// The default implementation ignores <paramref name="kind"/> and forwards to
    /// <see cref="Invoke(object, IValidator, string, CancellationToken)"/>, so implementations written before this
    /// overload existed keep working unchanged. Every parameter is required so existing calls such as
    /// <c>Invoke(instance, validator, path)</c> and <c>Invoke(instance, validator, path, default)</c> keep binding to the original method.
    /// </remarks>
    Task<IEnumerable<ValidationResult>> Invoke(object instance, IValidator validator, string path, ValidationTargetKind? kind, CancellationToken cancellationToken) =>
        Invoke(instance, validator, path, cancellationToken);
}
