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
    /// Defers to <see cref="QueryArgumentsModelConvention"/>, the single source both this and the proxy generator
    /// compile in, so the client and the server cannot resolve different types for the same query. The performer's
    /// parameters are used rather than the raw method signature, which is what excludes injected dependencies.
    /// </remarks>
    static Type? ResolveModelTypeFor(IQueryPerformer performer) =>
        QueryArgumentsModelConvention.Resolve(
            performer.ReadModelType.Name,
            performer.Name,
            [.. performer.Parameters.Select(_ => new QueryArgumentDescriptor(_.Name, _.Type))],
            performer.ReadModelType.Assembly.GetTypes());

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
