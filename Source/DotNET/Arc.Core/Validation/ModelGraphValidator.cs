// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Cratis.DependencyInjection;
using Cratis.Reflection;
using Cratis.Strings;
using FluentValidation;

namespace Cratis.Arc.Validation;

/// <summary>
/// Represents an implementation of <see cref="IModelGraphValidator"/>.
/// </summary>
/// <param name="discoverableValidators">The <see cref="IDiscoverableValidators"/> to use for finding validators.</param>
/// <param name="validatorInvoker">The <see cref="IValidatorInvoker"/> to run a discovered validator with.</param>
[Singleton]
public class ModelGraphValidator(IDiscoverableValidators discoverableValidators, IValidatorInvoker validatorInvoker) : IModelGraphValidator
{
    /// <summary>
    /// Caches the properties worth walking per type. Reflecting over a type's members is the dominant cost of a
    /// traversal, and queries run this on every request — unlike commands, which are comparatively rare. The set of
    /// properties for a type never changes, so it is resolved once and reused for the lifetime of the process.
    /// </summary>
    static readonly ConcurrentDictionary<Type, PropertyInfo[]> _walkableProperties = new();

    /// <summary>
    /// Caches whether a type is a leaf, for the same reason.
    /// </summary>
    static readonly ConcurrentDictionary<Type, bool> _leafTypes = new();

    /// <inheritdoc/>
    public async Task<IEnumerable<ValidationResult>> Validate(ModelGraphValidationRequest request, CancellationToken cancellationToken = default)
    {
        var results = new List<ValidationResult>();
        await Validate(request, request.Instance, request.RootPath, new HashSet<object>(ReferenceEqualityComparer.Instance), results, cancellationToken);
        return results;
    }

    /// <summary>
    /// Extends a member path with a property, in the casing the client uses.
    /// </summary>
    /// <param name="path">The path so far, or empty at the root.</param>
    /// <param name="property">The property being descended into.</param>
    /// <returns>The extended path.</returns>
    static string Extend(string path, PropertyInfo property) =>
        string.IsNullOrEmpty(path) ? property.Name.ToCamelCase() : $"{path}.{property.Name.ToCamelCase()}";

    /// <summary>
    /// Determines whether a type is a leaf for traversal purposes — a single value whose public properties describe
    /// its internals rather than further model to validate.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to check.</param>
    /// <returns>True when the type should not be descended into; otherwise false.</returns>
    /// <remarks>
    /// This defers to the framework's own answer to "is this a single value" rather than keeping a private list of
    /// types, which drifts: a hand-maintained list here already omitted <see cref="DateOnly"/> and
    /// <see cref="TimeOnly"/>, so the walker reflected over their calendar components on every request. Deferring
    /// means new value types are picked up wherever the rest of the framework picks them up.
    /// </remarks>
    static bool IsLeaf(Type type) =>
        _leafTypes.GetOrAdd(type, static _ => _.IsAPrimitiveType() || _.IsEnum);

    /// <summary>
    /// Gets the properties of a type that are worth walking.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to get properties for.</param>
    /// <returns>The properties to descend into.</returns>
    /// <remarks>
    /// Indexer properties are excluded: they require index arguments, so reading one without any would throw
    /// "Parameter count mismatch". They show up on types such as <c>JsonElement</c> (<c>this[int]</c>) that can
    /// appear in an object-typed property graph.
    /// </remarks>
    static PropertyInfo[] GetWalkableProperties(Type type) =>
        _walkableProperties.GetOrAdd(type, static _ => [.. _.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.GetIndexParameters().Length == 0)]);

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
            results.AddRange(await validatorInvoker.Invoke(instance, validator, path, cancellationToken));
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

        foreach (var property in GetWalkableProperties(instanceType))
        {
            var propertyValue = property.GetValue(instance);
            if (propertyValue is not null)
            {
                await Validate(request, propertyValue, Extend(path, property), visited, results, cancellationToken);
            }
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
