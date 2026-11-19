// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Neo4j.Driver;

namespace Cratis.Arc.Neo4j.Chronicle.Integration;

/// <summary>
/// Describes the live Neo4j server the opt-in integration specifications run against.
/// </summary>
/// <remarks>
/// They run only when <see cref="UriVariable"/>, <see cref="UserVariable"/> and <see cref="PasswordVariable"/> are set, so a
/// build without a Neo4j server skips them. They use single database mode, which works on an edition with one database, and
/// give every run its own namespace, which they remove again. The password is only ever passed on to the driver.
/// </remarks>
public static class LiveNeo4j
{
    /// <summary>
    /// The environment variable holding the Bolt URI of the server, for instance <c>bolt://localhost:7687</c>.
    /// </summary>
    public const string UriVariable = "NEO4J_TEST_URI";

    /// <summary>
    /// The environment variable holding the user name.
    /// </summary>
    public const string UserVariable = "NEO4J_TEST_USER";

    /// <summary>
    /// The environment variable holding the password.
    /// </summary>
    public const string PasswordVariable = "NEO4J_TEST_PASSWORD";

    /// <summary>
    /// The environment variable holding the database to use. Defaults to <c>neo4j</c>.
    /// </summary>
    public const string DatabaseVariable = "NEO4J_TEST_DATABASE";

    /// <summary>
    /// Gets a value indicating whether a live server is configured.
    /// </summary>
    public static bool Enabled => new[] { UriVariable, UserVariable, PasswordVariable }.All(_ => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(_)));

    /// <summary>
    /// Gets the reason the live specifications are skipped.
    /// </summary>
    public static string SkipReason => $"Live Neo4j specs are opt-in. Set {UriVariable}, {UserVariable} and {PasswordVariable} (and optionally {DatabaseVariable}) to run them.";

    /// <summary>
    /// Gets the database to use.
    /// </summary>
    public static string Database => Environment.GetEnvironmentVariable(DatabaseVariable) is { Length: > 0 } database ? database : "neo4j";

    /// <summary>
    /// Create a driver for the server.
    /// </summary>
    /// <returns>The <see cref="IDriver"/>.</returns>
    public static IDriver CreateDriver() =>
        GraphDatabase.Driver(
            Environment.GetEnvironmentVariable(UriVariable),
            AuthTokens.Basic(Environment.GetEnvironmentVariable(UserVariable), Environment.GetEnvironmentVariable(PasswordVariable)));
}
