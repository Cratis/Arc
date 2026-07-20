// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents an implementation of <see cref="IQueryArgumentsModels"/> that resolves the arguments model by
/// convention.
/// </summary>
/// <remarks>
/// The model is a type named <c>{QueryName}Parameters</c> or <c>{ReadModelName}{QueryName}Parameters</c> in the read
/// model's assembly, whose properties cover every parameter of the query. The same convention is what the proxy
/// generator uses to find the rules it emits into the client, so both sides resolve the same type and validate the
/// same shape.
/// </remarks>
/// <param name="logger">The <see cref="ILogger{TCategoryName}"/> used to log a model that cannot be materialized.</param>
[Singleton]
public class QueryArgumentsModels(ILogger<QueryArgumentsModels> logger) : IQueryArgumentsModels
{
    readonly ConcurrentDictionary<FullyQualifiedQueryName, Type?> _modelTypesByQuery = new();

    /// <inheritdoc/>
    public bool TryCreateFor(IQueryPerformer performer, QueryArguments arguments, [MaybeNullWhen(false)] out object model)
    {
        model = null;

        try
        {
            var modelType = _modelTypesByQuery.GetOrAdd(performer.FullyQualifiedName, static (_, queryPerformer) => ResolveModelTypeFor(queryPerformer), performer);
            if (modelType is null)
            {
                return false;
            }

            model = Materialize(modelType, arguments);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Resolving or materializing the model touches reflection over a caller-supplied argument set: a
            // partially loadable assembly, an unconvertible argument, or a constructor with guard clauses can all
            // throw. None of those are server faults the caller should see as a 500 — fall back to validating each
            // argument on its own, which still applies every validator that can be found for the values themselves.
            logger.CouldNotMaterializeArgumentsModel(performer.FullyQualifiedName.ToString() ?? string.Empty, ex);
            model = null;
        }

        return model is not null;
    }

    /// <summary>
    /// Resolves the arguments model type for a query, or null when it has none.
    /// </summary>
    /// <param name="performer">The <see cref="IQueryPerformer"/> for the query.</param>
    /// <returns>The model <see cref="Type"/>, or null.</returns>
    /// <remarks>
    /// A candidate only counts when it has a property for every one of the query's parameters, so an unrelated type
    /// that happens to carry the name is never picked up. Verifying against the performer's parameters rather than
    /// the raw method signature means injected dependencies are correctly ignored.
    /// <para>
    /// Every type carrying a candidate name is considered, not just the first one found. Two read models can each
    /// expose a query of the same name, which makes <c>{QueryName}Parameters</c> ambiguous; picking one arbitrarily
    /// and giving up when its shape did not match would resolve differently depending on the order the runtime
    /// happens to report types in, and silently skip validation for whichever query lost.
    /// </para>
    /// </remarks>
    static Type? ResolveModelTypeFor(IQueryPerformer performer)
    {
        // A query with no arguments has nothing for a model to cover, and "covers every parameter" is vacuously true
        // for an empty parameter set — without this, a parameterless query named All would bind any stray
        // AllParameters type in the assembly and validate a shape the developer never associated with it.
        if (performer.Parameters.Count == 0)
        {
            return null;
        }

        var readModelType = performer.ReadModelType;
        string[] candidateNames =
        [
            $"{readModelType.Name}{performer.Name}Parameters",
            $"{performer.Name}Parameters"
        ];

        var types = readModelType.Assembly.GetTypes();

        foreach (var candidateName in candidateNames)
        {
            var candidate = types.FirstOrDefault(type =>
                type.Name.Equals(candidateName, StringComparison.OrdinalIgnoreCase) && CoversEveryParameter(type, performer));

            if (candidate is not null)
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// Determines whether a candidate type has a property of matching name and type for every parameter the query
    /// exposes.
    /// </summary>
    /// <param name="candidate">The candidate <see cref="Type"/>.</param>
    /// <param name="performer">The <see cref="IQueryPerformer"/> whose parameters must be covered.</param>
    /// <returns>True when every parameter is covered; otherwise false.</returns>
    /// <remarks>
    /// The type is matched as well as the name, and deliberately so: a name-only match would accept a model whose
    /// members cannot hold the arguments, which then fails while being materialized. Requiring the type means a
    /// mismatched candidate is simply not the argument model, and validation falls back to each argument on its own.
    /// This is the same criterion the proxy generator applies, so both sides resolve the same type.
    /// </remarks>
    static bool CoversEveryParameter(Type candidate, IQueryPerformer performer)
    {
        var properties = candidate.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        return performer.Parameters.All(parameter =>
            properties.Any(property =>
                property.Name.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase) &&
                property.PropertyType == parameter.Type));
    }

    /// <summary>
    /// Materializes the model from the arguments, filling what is present and leaving the rest at its default.
    /// </summary>
    /// <param name="modelType">The model <see cref="Type"/> to materialize.</param>
    /// <param name="arguments">The <see cref="QueryArguments"/> to fill it from.</param>
    /// <returns>The materialized instance, or null when it could not be constructed.</returns>
    /// <remarks>
    /// Handles both a positional record and a class with settable properties: the longest constructor is satisfied
    /// from the arguments by name, then any remaining writable property is filled the same way. An argument that is
    /// absent leaves its member at the type's default, which is what the validator should then reject.
    /// </remarks>
    static object? Materialize(Type modelType, QueryArguments arguments)
    {
        var constructor = modelType.GetConstructors()
            .OrderByDescending(_ => _.GetParameters().Length)
            .FirstOrDefault();

        if (constructor is null)
        {
            return null;
        }

        var constructorArguments = constructor.GetParameters()
            .Select(parameter => ValueFor(parameter.Name, parameter.ParameterType, arguments))
            .ToArray();

        var instance = constructor.Invoke(constructorArguments);

        foreach (var property in modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanWrite || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            if (arguments.TryGetValue(property.Name, out var value) && value is not null)
            {
                property.SetValue(instance, value.ConvertTo(property.PropertyType));
            }
        }

        return instance;
    }

    /// <summary>
    /// Gets the value for a member from the arguments, converted to the member's type.
    /// </summary>
    /// <param name="name">The member name to look up.</param>
    /// <param name="type">The member type to convert to.</param>
    /// <param name="arguments">The <see cref="QueryArguments"/> to look in.</param>
    /// <returns>The converted value, or the type's default when the argument is absent.</returns>
    static object? ValueFor(string? name, Type type, QueryArguments arguments) =>
        name is not null && arguments.TryGetValue(name, out var value) && value is not null
            ? value.ConvertTo(type)
            : DefaultFor(type);

    /// <summary>
    /// Gets the default value for a type.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to get the default for.</param>
    /// <returns>The default value.</returns>
    static object? DefaultFor(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;
}
