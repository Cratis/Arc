// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using System.Runtime.CompilerServices;
using Cratis.Arc.Connections;
using Cratis.Arc.Tenancy;
using Cratis.DependencyInjection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Cratis.Arc.EntityFrameworkCore.Connections;

/// <summary>
/// Represents a <see cref="DbConnectionInterceptor"/> that resolves the connection for the current tenant and purpose
/// every time a connection opens.
/// </summary>
/// <remarks>
/// <para>
/// Resolving when the connection opens rather than when the context is created is what makes this work with pooled
/// <c>DbContext</c> factories: a pooled context keeps its connection object, but every lease opens it again for whoever
/// is leasing it. The resolution is applied over the connection string the context was configured with, never over one
/// resolved for an earlier lease.
/// </para>
/// <para>
/// The purpose is <see cref="ConnectionPurpose.Runtime"/> unless <see cref="ConnectionPurposeScope"/> says otherwise. When
/// a connection fails to authenticate, the cached resolution is dropped so the next attempt picks up rotated credentials.
/// </para>
/// </remarks>
/// <param name="resolver">The <see cref="ICachingConnectionResolver"/> to resolve through.</param>
/// <param name="tenantIdAccessor">The <see cref="ITenantIdAccessor"/> for the current tenant.</param>
[IgnoreConvention]
public class ConnectionResolutionInterceptor(ICachingConnectionResolver resolver, ITenantIdAccessor tenantIdAccessor) : DbConnectionInterceptor
{
    /// <summary>
    /// SQL Server error number for a failed login.
    /// </summary>
    public const int SqlServerLoginFailed = 18456;

    /// <summary>
    /// PostgreSQL SQL states for failed authentication: invalid authorization specification and invalid password.
    /// </summary>
    public static readonly IReadOnlyCollection<string> PostgreSqlAuthenticationFailed = ["28000", "28P01"];

    readonly ConditionalWeakTable<DbConnection, string> _configured = [];

    /// <summary>
    /// Check whether an exception means the connection failed to authenticate.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/>.</param>
    /// <returns>True if it is an authentication failure, false if not.</returns>
    public static bool IsAuthenticationFailure(Exception? exception) => exception switch
    {
        null => false,
        SqlException sql => sql.Errors.Cast<SqlError>().Any(_ => _.Number == SqlServerLoginFailed),
        PostgresException postgres => PostgreSqlAuthenticationFailed.Contains(postgres.SqlState),
        _ => IsAuthenticationFailure(exception.InnerException)
    };

    /// <inheritdoc/>
    public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        // EF Core opens synchronously here, so the resolution is waited for. It is cached, so this only waits when the
        // cache is empty or expired.
        Apply(connection, resolver.Resolve(RequestFor(connection)).GetAwaiter().GetResult());
        return result;
    }

    /// <inheritdoc/>
    public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
    {
        Apply(connection, await resolver.Resolve(RequestFor(connection), cancellationToken));
        return result;
    }

    /// <inheritdoc/>
    public override void ConnectionFailed(DbConnection connection, ConnectionErrorEventData eventData) =>
        InvalidateOnAuthenticationFailure(connection, eventData.Exception);

    /// <inheritdoc/>
    public override Task ConnectionFailedAsync(DbConnection connection, ConnectionErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        InvalidateOnAuthenticationFailure(connection, eventData.Exception);
        return Task.CompletedTask;
    }

    ConnectionRequest RequestFor(DbConnection connection) =>
        ConnectionRequest.ForTenant(connection.GetConnectionStorage(), ConnectionPurposeScope.Current, tenantIdAccessor.Current);

    void Apply(DbConnection connection, ResolvedConnection resolved)
    {
        var configured = _configured.GetValue(connection, static _ => _.ConnectionString);
        connection.ApplyResolvedConnection(configured, resolved);
    }

    void InvalidateOnAuthenticationFailure(DbConnection connection, Exception exception)
    {
        if (IsAuthenticationFailure(exception))
        {
            resolver.Invalidate(RequestFor(connection));
        }
    }
}
