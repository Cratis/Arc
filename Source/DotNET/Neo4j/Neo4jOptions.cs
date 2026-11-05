// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Connections;

namespace Cratis.Arc.Neo4j;

/// <summary>
/// Represents the configuration for Neo4j.
/// </summary>
public class Neo4jOptions
{
    /// <summary>
    /// The default name of the graph database in <see cref="GraphDatabaseMode.Single"/> mode.
    /// </summary>
    public const string DefaultSingleDatabase = "neo4j";

    /// <summary>
    /// Gets or sets the server URI, for instance <c>bolt://localhost:7687</c> or <c>neo4j+s://graph.example</c>.
    /// </summary>
    [Required]
    public string Server { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the event store whose graph is read. It is the first part of the database name.
    /// </summary>
    /// <remarks>
    /// It must be the event store Chronicle's Neo4j sink writes, so Arc reads the database that sink created.
    /// </remarks>
    [Required]
    public string EventStore { get; set; } = null!;

    /// <summary>
    /// Gets or sets the optional user name to authenticate with.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the password to authenticate with. Prefer <see cref="PasswordSecret"/>.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the name of the configuration key holding the password, for instance <c>Neo4j:Password</c> or an
    /// environment variable name such as <c>NEO4J_PASSWORD</c>.
    /// </summary>
    /// <remarks>
    /// The key is read from the application's <c>IConfiguration</c> when the connection is made, so the password can
    /// come from any configuration provider — user secrets, environment variables or a key vault — and never has to
    /// be written in the options. When both are set, the secret wins.
    /// </remarks>
    public string? PasswordSecret { get; set; }

    /// <summary>
    /// Gets or sets the optional prefix of the database names, for sharing one Neo4j server between applications.
    /// </summary>
    public string? DatabasePrefix { get; set; }

    /// <summary>
    /// Gets or sets how the graphs of the namespaces are placed. Defaults to <see cref="GraphDatabaseMode.PerNamespace"/>.
    /// </summary>
    public GraphDatabaseMode DatabaseMode { get; set; } = GraphDatabaseMode.PerNamespace;

    /// <summary>
    /// Gets or sets the name of the database used in <see cref="GraphDatabaseMode.Single"/> mode. Defaults to <see cref="DefaultSingleDatabase"/>.
    /// </summary>
    public string SingleDatabase { get; set; } = DefaultSingleDatabase;

    /// <summary>
    /// Gets or sets how connections are resolved per tenant, overriding the application-wide
    /// <see cref="ArcOptions.ConnectionResolution"/> for Neo4j.
    /// </summary>
    /// <remarks>
    /// When neither is configured, <see cref="Server"/>, the credentials and the database name are used as they are. A
    /// resolved connection string replaces <see cref="Server"/>, a resolved database replaces the database name, and a
    /// resolved identity becomes the credential — a user name and password, or a bearer token.
    /// </remarks>
    public ConnectionResolutionOptions ConnectionResolution { get; set; } = new();
}
