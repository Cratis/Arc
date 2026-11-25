// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Provides a read model as a context value for everything in the namespace of the type that carries the attribute.
/// </summary>
/// <remarks>
/// <para>
/// The read model is resolved by a static query method, keyed from the arguments of the command or query being decided.
/// It is provided as <c>$context.&lt;name&gt;</c>, where the name is the name of the read model type in camelCase, and is the
/// Screenplay <c>provide</c> line. Put the attribute on a type in the namespace of the module, feature or slice the value is
/// provided for, usually the read model or an <see cref="AvailabilityScope"/> marker.
/// </para>
/// <para>
/// The host registers an <c>IReadModelQuery</c> that executes the query.
/// </para>
/// </remarks>
/// <typeparam name="TReadModel">The read model that is provided.</typeparam>
/// <param name="query">The name of the static query method of the read model, which returns one read model.</param>
/// <param name="key">The argument of the command or query that keys it, for example <c>arguments.vesselId</c>.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class ProvideContextAttribute<TReadModel>(string query, string key) : Attribute
{
    /// <summary>
    /// Gets the name of the query that resolves the read model.
    /// </summary>
    public string Query { get; } = query;

    /// <summary>
    /// Gets the argument that keys the query.
    /// </summary>
    public string Key { get; } = key;

    /// <summary>
    /// Gets or sets a value indicating whether the value is required. An optional value that is absent is unknown, never an error.
    /// </summary>
    public bool Required { get; set; } = true;

    /// <summary>
    /// Gets the type of the read model.
    /// </summary>
    public Type ReadModel { get; } = typeof(TReadModel);
}
