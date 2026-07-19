// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Cratis.DependencyInjection;
using Cratis.Strings;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Validation;

/// <summary>
/// Represents an implementation of <see cref="IModelGraphValidator"/>.
/// </summary>
/// <param name="discoverableValidators">The <see cref="IDiscoverableValidators"/> to use for finding validators.</param>
/// <param name="logger">The <see cref="ILogger{TCategoryName}"/> used to log a validator that throws while validating.</param>
[Singleton]
public class ModelGraphValidator(IDiscoverableValidators discoverableValidators, ILogger<ModelGraphValidator> logger) : IModelGraphValidator
{
    /// <inheritdoc/>
    public async Task<IEnumerable<ValidationResult>> Validate(ModelGraphValidationRequest request, CancellationToken cancellationToken = default)
    {
        var results = new List<ValidationResult>();
        await Validate(request, request.Instance, request.RootPath, new HashSet<object>(ReferenceEqualityComparer.Instance), results, cancellationToken);
        return results;
    }

    /// <summary>
    /// Prefixes a validation failure member with the owning property path, producing a dotted path whose leading
    /// segment is the field the client supplied (for example <c>email.Value</c>). At an empty path the member is
    /// returned unchanged.
    /// </summary>
    /// <param name="path">The camelCased property path from the graph root, or empty at the root.</param>
    /// <param name="member">The failure member reported by the validator.</param>
    /// <returns>The member prefixed with <paramref name="path"/>, or the member unchanged when the path is empty.</returns>
    static string Combine(string path, string member) =>
        string.IsNullOrEmpty(path) ? member : $"{path}.{member}";

    /// <summary>
    /// Determines whether a type is a leaf for traversal purposes — a value whose public properties describe its
    /// internals rather than further model to validate.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to check.</param>
    /// <returns>True when the type should not be descended into; otherwise false.</returns>
    static bool IsLeaf(Type type) =>
        type.IsPrimitive ||
        type == typeof(string) ||
        type == typeof(DateTime) ||
        type == typeof(DateTimeOffset) ||
        type == typeof(Guid) ||
        type == typeof(decimal);

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

    async Task Validate(
        ModelGraphValidationRequest request,
        object instance,
        string path,
        HashSet<object> visited,
        List<ValidationResult> results,
        CancellationToken cancellationToken)
    {
        var instanceType = instance.GetType();

        // Guard against cycles in arbitrary object graphs. Some types — notably
        // System.Text.Json.Nodes.JsonNode — hold a back-reference from every child to its parent, so
        // blindly walking child properties would recurse forever and overflow the stack. Only reference
        // types can participate in a cycle; value types are boxed afresh on each access, so tracking them
        // would never dedupe and would only add overhead. ReferenceEqualityComparer keys on identity, so
        // distinct-but-equal instances (e.g. two equal concept values in a list) are still each validated.
        if (!instanceType.IsValueType && !visited.Add(instance))
        {
            return;
        }

        if (TryGetValidator(request.ServiceProvider, instanceType, out var validator))
        {
            results.AddRange(await RunValidator(request, instance, validator, path, cancellationToken));
        }

        if (IsLeaf(instanceType))
        {
            return;
        }

        if (instanceType.IsArray || typeof(IEnumerable).IsAssignableFrom(instanceType))
        {
            foreach (var element in (IEnumerable)instance)
            {
                if (element is null) continue;
                await Validate(request, element, path, visited, results, cancellationToken);
            }

            return;
        }

        foreach (var property in instanceType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Skip indexer properties — they require index arguments, so GetValue(instance)
            // without any would throw "Parameter count mismatch". These show up on types such as
            // JsonElement (this[int]) that can appear in an object-typed property graph.
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            var propertyValue = property.GetValue(instance);
            if (propertyValue is not null)
            {
                await Validate(request, propertyValue, Combine(path, property.Name.ToCamelCase()), visited, results, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Runs a resolved validator against an instance, converting a validator that throws while validating
    /// (for example a rule that dereferences a null concept member such as <c>RuleFor(c =&gt; c.X.Value)</c> on a
    /// null <c>X</c>) into a validation failure (HTTP 400) instead of letting it propagate as a server error (HTTP 500).
    /// </summary>
    /// <param name="request">The <see cref="ModelGraphValidationRequest"/> the validation runs within.</param>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="validator">The <see cref="IValidator"/> to run.</param>
    /// <param name="path">The camelCased property path from the graph root to <paramref name="instance"/>. Each failure's
    /// member is prefixed with it so a rule on a nested value — for example a <c>ConceptValidator&lt;T&gt;</c>'s
    /// <c>RuleFor(x =&gt; x.Value)</c>, which reports the inner member <c>Value</c> — is attributed to the owning field
    /// (<c>email.Value</c>) rather than the unattributable <c>Value</c>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> for cancelling the validation.</param>
    /// <returns>The <see cref="ValidationResult"/> collection describing the outcome.</returns>
    async Task<IEnumerable<ValidationResult>> RunValidator(
        ModelGraphValidationRequest request,
        object instance,
        IValidator validator,
        string path,
        CancellationToken cancellationToken)
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

            return validationResult.Errors.Select(_ =>
                new ValidationResult(ToSeverity(_.Severity), _.ErrorMessage, [Combine(path, _.PropertyName)], _.CustomState ?? null!)).ToArray();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A validator that dereferences a null concept member throws while validating hostile or partial
            // input. Surface it as a validation failure (HTTP 400) rather than letting it propagate to a server
            // error (HTTP 500). The detail is logged server-side and never returned to the client. Cancellation
            // is deliberately excluded so a cancelled request is not mistaken for invalid input.
            logger.ValidatorThrew(instance.GetType().FullName ?? instance.GetType().Name, ex);
            return [ValidationResult.Error(request.MessageWhenValidatorThrows)];
        }
    }

    /// <summary>
    /// Resolves a validator for the given model type, preferring the operation-scoped <see cref="IServiceProvider"/>
    /// so the validator and its dependencies resolve from the same scope as the operation being validated.
    /// </summary>
    /// <param name="serviceProvider">The optional scoped <see cref="IServiceProvider"/> to resolve from.</param>
    /// <param name="modelType">The type to resolve a validator for.</param>
    /// <param name="validator">The resolved <see cref="IValidator"/> when found.</param>
    /// <returns>True if a validator was found; otherwise false.</returns>
    bool TryGetValidator(IServiceProvider? serviceProvider, Type modelType, [MaybeNullWhen(false)] out IValidator validator) =>
        serviceProvider is { } provider
            ? discoverableValidators.TryGet(modelType, provider, out validator)
            : discoverableValidators.TryGet(modelType, out validator);
}
