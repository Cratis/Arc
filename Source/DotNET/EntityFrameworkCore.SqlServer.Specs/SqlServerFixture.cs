// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using Testcontainers.MsSql;

namespace Cratis.Arc.EntityFrameworkCore.SqlServer;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable MA0048 // File name must match type name

/// <summary>
/// Reusable xUnit fixture that provides a shared SQL Server container across tests.
/// The container is started once and reused for all tests to avoid startup overhead.
/// </summary>
public class SqlServerFixture : IAsyncLifetime
{
    const string Password = "YourStrong@Passw0rd";

    MsSqlContainer _container = null!;

    /// <summary>
    /// Gets the connection string to the SQL Server container.
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword(Password)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_SA_PASSWORD", Password)
            .WithPortBinding(1433, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
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
/// Collection definition for SQL Server integration tests.
/// Tests sharing this collection will share the same SQL Server container.
/// </summary>
[CollectionDefinition(Name)]
public class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    /// <summary>
    /// The collection name to use in [Collection] attribute.
    /// </summary>
    public const string Name = "SQL Server";
}
