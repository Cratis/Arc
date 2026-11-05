// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using Neo4j.Driver;

namespace Cratis.Arc.Neo4j;

/// <summary>
/// Defines a session on the graph database of the current tenant.
/// </summary>
/// <remarks>
/// <para>
/// The scoped <see cref="IGraphSession"/> is a read session: it opens the database of the tenant with <see cref="AccessMode.Read"/>,
/// so a query that writes is refused by the server. Inject <see cref="IGraphSessionFactory"/> for the rare case that needs to write.
/// </para>
/// <para>
/// Parameters may be given as an object or a dictionary. <see cref="ConceptAs{T}"/> values bind as their primitive, and
/// results are mapped to concept-typed properties; see <see cref="GraphParameterConverter"/> and <see cref="GraphRecordMapper"/>.
/// Every query is also given the <see cref="GraphParameters.Namespace"/> parameter.
/// </para>
/// </remarks>
public interface IGraphSession : IAsyncDisposable
{
    /// <summary>
    /// Gets the name of the database the session is on.
    /// </summary>
    string DatabaseName { get; }

    /// <summary>
    /// Gets the name of the namespace of the current tenant.
    /// </summary>
    string Namespace { get; }

    /// <summary>
    /// Gets how the graphs of the namespaces are placed.
    /// </summary>
    GraphDatabaseMode DatabaseMode { get; }

    /// <summary>
    /// Gets the <see cref="AccessMode"/> of the session.
    /// </summary>
    AccessMode AccessMode { get; }

    /// <summary>
    /// Gets the driver's session, for what this interface does not cover, such as transaction functions.
    /// </summary>
    IAsyncSession Session { get; }

    /// <summary>
    /// Get the parameters for a query: the given ones converted for the driver, plus <see cref="GraphParameters.Namespace"/>.
    /// </summary>
    /// <param name="parameters">Null, a dictionary, or an object whose public properties are the parameters.</param>
    /// <returns>The parameters to give the driver.</returns>
    /// <remarks>
    /// Use it when you run a query through <see cref="Session"/> yourself. In <see cref="GraphDatabaseMode.Single"/> mode,
    /// filter every pattern with <c>WHERE n._namespace = $namespace</c>.
    /// </remarks>
    IDictionary<string, object> Parameters(object? parameters = null);

    /// <summary>
    /// Run a query and give the cursor over its records.
    /// </summary>
    /// <param name="cypher">The Cypher text.</param>
    /// <param name="parameters">Optional parameters.</param>
    /// <returns>The <see cref="IResultCursor"/>.</returns>
    Task<IResultCursor> Run(string cypher, object? parameters = null);

    /// <summary>
    /// Run a query and map every record to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="cypher">The Cypher text.</param>
    /// <param name="parameters">Optional parameters.</param>
    /// <returns>The mapped records.</returns>
    Task<IReadOnlyList<T>> Query<T>(string cypher, object? parameters = null);

    /// <summary>
    /// Run a query and map the first record to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="cypher">The Cypher text.</param>
    /// <param name="parameters">Optional parameters.</param>
    /// <returns>The mapped record, or null when the query returned none.</returns>
    Task<T?> QuerySingle<T>(string cypher, object? parameters = null);
}
