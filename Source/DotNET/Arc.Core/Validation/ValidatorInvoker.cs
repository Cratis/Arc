// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Cratis.Strings;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Validation;

/// <summary>
/// Represents an implementation of <see cref="IValidatorInvoker"/> for FluentValidation.
/// </summary>
/// <param name="logger">The <see cref="ILogger{TCategoryName}"/> used to log a validator that throws while validating.</param>
[Singleton]
public class ValidatorInvoker(ILogger<ValidatorInvoker> logger) : IValidatorInvoker
{
    /// <summary>
    /// The message surfaced when a validator throws while validating.
    /// </summary>
    /// <remarks>
    /// Deliberately says nothing about what went wrong: a validator throws on hostile or partial input, and the
    /// detail is logged server-side rather than handed back to whoever sent it. It reads the same for a command and
    /// for a query because the distinction tells a client nothing it can act on.
    /// </remarks>
    public const string CouldNotValidateMessage = "The value could not be validated.";

    /// <inheritdoc/>
    public async Task<IEnumerable<ValidationResult>> Invoke(object instance, IValidator validator, string path, CancellationToken cancellationToken = default)
    {
        try
        {
            var validationContextType = typeof(ValidationContext<>).MakeGenericType(instance.GetType());
            var validationContext = Activator.CreateInstance(validationContextType, instance) as IValidationContext;
            var validationResult = await validator.ValidateAsync(validationContext, cancellationToken);
            if (validationResult.IsValid)
            {
                return [];
            }

            var isConcept = instance.GetType().IsConcept();
            return validationResult.Errors.Select(_ =>
                new ValidationResult(ToSeverity(_.Severity), _.ErrorMessage, [MemberFor(path, _.PropertyName, isConcept)], _.CustomState ?? null!)).ToArray();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A validator that dereferences a null concept member throws while validating hostile or partial
            // input. Surface it as a validation failure (HTTP 400) rather than letting it propagate to a server
            // error (HTTP 500). The detail is logged server-side and never returned to the client. Cancellation
            // is deliberately excluded so a cancelled request is not mistaken for invalid input.
            logger.ValidatorThrew(instance.GetType().FullName ?? instance.GetType().Name, ex);
            return [ValidationResult.Error(CouldNotValidateMessage)];
        }
    }

    /// <summary>
    /// Builds the member a validation failure is attributed to.
    /// </summary>
    /// <param name="path">The camelCased member path from the graph root, or empty at the root.</param>
    /// <param name="member">The member the validator reported.</param>
    /// <param name="isConcept">Whether the failure was reported by a validator for a concept.</param>
    /// <returns>The member name to report.</returns>
    /// <remarks>
    /// Members are camelCased because that is how the client names them — the generated proxy models everything in
    /// TypeScript casing, so reporting <c>Email</c> where the client reports <c>email</c> means a form cannot match a
    /// server failure to the field that caused it.
    /// <para>
    /// A concept's own member is dropped. A <c>ConceptValidator&lt;T&gt;</c> declares its rules against the concept's
    /// inner <c>Value</c>, but a concept is a single value: the failure belongs to the field holding it. TypeScript
    /// erases the concept to its primitive and reports <c>email</c>, and this reports the same.
    /// </para>
    /// </remarks>
    static string MemberFor(string path, string member, bool isConcept)
    {
        if (isConcept)
        {
            return string.IsNullOrEmpty(path) ? member.ToCamelCase() : path;
        }

        var camelCased = member.ToCamelCase();
        return string.IsNullOrEmpty(path) ? camelCased : $"{path}.{camelCased}";
    }

    /// <summary>
    /// Maps a FluentValidation <see cref="Severity"/> onto the framework's <see cref="ValidationResultSeverity"/>.
    /// </summary>
    /// <param name="severity">The FluentValidation <see cref="Severity"/> to map.</param>
    /// <returns>The corresponding <see cref="ValidationResultSeverity"/>.</returns>
    static ValidationResultSeverity ToSeverity(Severity severity) => severity switch
    {
        Severity.Info => ValidationResultSeverity.Information,
        Severity.Warning => ValidationResultSeverity.Warning,
        Severity.Error => ValidationResultSeverity.Error,
        _ => ValidationResultSeverity.Error
    };
}
