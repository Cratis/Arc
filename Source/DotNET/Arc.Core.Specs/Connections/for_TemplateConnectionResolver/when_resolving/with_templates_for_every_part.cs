// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_TemplateConnectionResolver.when_resolving;

public class with_templates_for_every_part : given.a_request_for_a_tenant
{
    ResolvedConnection _result;

    async Task Because() => _result = await new TemplateConnectionResolver(new()
    {
        ConnectionString = "Server=sql.example;Database=app-{tenant}",
        Database = "app-{Tenant}",
        Schema = "{storage}_{purpose}"
    }).Resolve(_request);

    [Fact] void should_substitute_into_the_connection_string() => _result.ConnectionString.ShouldEqual("Server=sql.example;Database=app-acme");
    [Fact] void should_match_placeholders_without_regard_to_case() => _result.Database.ShouldEqual("app-acme");
    [Fact] void should_substitute_purpose_and_storage() => _result.Schema.ShouldEqual("SqlServer_Schema");
    [Fact] void should_not_set_an_identity() => _result.Identity.ShouldBeNull();
}
