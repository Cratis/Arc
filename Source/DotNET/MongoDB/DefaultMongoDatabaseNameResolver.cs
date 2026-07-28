// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IMongoDatabaseNameResolver"/> that resolves a configured database name with tenant suffix.
/// </summary>
/// <param name="options">The <see cref="IOptionsMonitor{TOptions}"/>.</param>
/// <param name="tenantIdAccessor">The <see cref="ITenantIdAccessor"/>.</param>
public class DefaultMongoDatabaseNameResolver(IOptionsMonitor<MongoDBOptions> options, ITenantIdAccessor tenantIdAccessor) : IMongoDatabaseNameResolver
{
    /// <inheritdoc/>
    /// <remarks>
    /// The default tenant — unset, or named <see cref="TenantId.Default"/> — resolves the bare database name.
    /// Suffixing it instead would address a database that nothing writes to, and a read of a database that does
    /// not exist returns no rows rather than failing, so the mistake surfaces as silently empty results.
    /// </remarks>
    public string Resolve()
    {
        var databaseName = options.CurrentValue.Database;
        var tenantId = tenantIdAccessor.Current;

        if (tenantId.IsDefault)
        {
            return databaseName;
        }

        return $"{databaseName}+{tenantId.Value}";
    }
}