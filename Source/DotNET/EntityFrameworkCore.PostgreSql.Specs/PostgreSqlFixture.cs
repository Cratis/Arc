// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using Testcontainers.PostgreSql;

namespace Cratis.Arc.EntityFrameworkCore.PostgreSql;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable MA0048 // File name must match type name

/// <summary>
/// Reusable xUnit fixture that provides a shared PostgreSQL container across tests.
/// The container is started once and reused for all tests to avoid startup overhead.
/// </summary>
public class PostgreSqlFixture : IAsyncLifetime
{
    const string Password = "YourStrong@Passw0rd";

    PostgreSqlContainer _container = null!;

    /// <summary>
    /// Gets the connection string to the PostgreSQL container.
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithPassword(Password)
            .WithPortBinding(5432, true)
            .WithWaitStrategy(Wait.ForUnixContainer())
            .Build();

        await _container.StartAsync();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

/// <summary>
/// Collection definition for PostgreSQL integration tests.
/// Tests sharing this collection will share the same PostgreSQL container.
/// </summary>
[CollectionDefinition(Name)]
public class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
    /// <summary>
    /// The collection name to use in [Collection] attribute.
    /// </summary>
    public const string Name = "PostgreSQL";
}
