// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.MongoDB.for_DefaultMongoDatabaseNameResolver.when_resolving;

public class and_tenant_id_is_not_set : Specification
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
        _tenantIdAccessor.Current.Returns(TenantId.NotSet);

        _resolver = new DefaultMongoDatabaseNameResolver(_options, _tenantIdAccessor);
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_database_name_without_tenant_suffix() => _result.ShouldEqual(_databaseName);
}
