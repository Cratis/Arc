// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.EntityFrameworkCore;

/// <summary>
/// DbContext options extension for storing the database type.
/// </summary>
/// <param name="databaseType">The database type to store.</param>
internal sealed class DatabaseTypeOptionsExtension(DatabaseType databaseType) : IDbContextOptionsExtension
{
    /// <summary>
    /// Gets the configured database type.
    /// </summary>
    public DatabaseType DatabaseType { get; } = databaseType;

    /// <inheritdoc/>
    public DbContextOptionsExtensionInfo Info => new ExtensionInfo(this);

    /// <inheritdoc/>
    public void ApplyServices(IServiceCollection services)
    {
    }

    /// <inheritdoc/>
    public void Validate(IDbContextOptions options)
    {
    }

#pragma warning disable IDE0290 // Use primary constructor - not possible due to base class constructor requirement
    sealed class ExtensionInfo : DbContextOptionsExtensionInfo
    {
        readonly DatabaseTypeOptionsExtension _extension;

        public ExtensionInfo(DatabaseTypeOptionsExtension extension)
            : base(extension) => _extension = extension;

        public override bool IsDatabaseProvider => false;

        public override string LogFragment => $"DatabaseType={_extension.DatabaseType}";

        public override int GetServiceProviderHashCode() => _extension.DatabaseType.GetHashCode();

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) =>
            other is ExtensionInfo otherInfo && otherInfo._extension.DatabaseType == _extension.DatabaseType;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            debugInfo["Cratis:DatabaseType"] = _extension.DatabaseType.ToString();
        }
    }
#pragma warning restore IDE0290
}
