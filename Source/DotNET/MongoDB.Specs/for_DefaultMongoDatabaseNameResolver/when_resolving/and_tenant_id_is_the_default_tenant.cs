// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.MongoDB.for_DefaultMongoDatabaseNameResolver.when_resolving;

/// <summary>
/// A host that names the default tenant explicitly must address the same database as one that resolves no tenant
/// at all. Chronicle materializes the default namespace's read models into the bare database name, so suffixing
/// this tenant addresses a database nothing writes to — and reading a database that does not exist returns no
/// rows rather than failing.
/// </summary>
public class and_tenant_id_is_the_default_tenant : Specification
{
    DefaultMongoDatabaseNameResolver _resolver;
    IOptionsMonitor<MongoDBOptions> _options;
    ITenantIdAccessor _tenantIdAccessor;
    string _databaseName;
    string _result;

    void Establish()
    {
        _databaseName = "test-database";

        _options = Substitute.For<IOptionsMonitor<MongoDBOptions>>();
        _options.CurrentValue.Returns(new MongoDBOptions { Database = _databaseName });

        _tenantIdAccessor = Substitute.For<ITenantIdAccessor>();
        _tenantIdAccessor.Current.Returns(TenantId.Default);

        _resolver = new DefaultMongoDatabaseNameResolver(_options, _tenantIdAccessor);
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_the_database_name_without_a_tenant_suffix() => _result.ShouldEqual(_databaseName);
}
