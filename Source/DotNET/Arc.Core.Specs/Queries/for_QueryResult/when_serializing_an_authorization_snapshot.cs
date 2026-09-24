// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Tenancy;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_QueryResult;

public class when_serializing_an_authorization_snapshot : Specification
{
    string _json = string.Empty;

    void Because()
    {
        var result = QueryResult.Success(CorrelationId.New());
        result.AuthorizedPrincipal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "private-principal")], "Special"));
        result.AuthorizedTenant = new TenantId("private-tenant");
        result.AuthorizedArguments = new QueryArguments { ["secret"] = "private-argument" };
        result.OwnedScope = Substitute.For<IServiceScope>();
        _json = JsonSerializer.Serialize(result, new ArcOptions().JsonSerializerOptions);
    }

    [Fact] void should_not_serialize_the_selected_principal() => _json.Contains("private-principal", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_serialize_the_selected_tenant() => _json.Contains("private-tenant", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_serialize_coerced_internal_arguments() => _json.Contains("private-argument", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_serialize_the_service_scope() => _json.Contains("ownedScope", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    [Fact] void should_keep_the_existing_public_result_shape() => _json.Contains("isAuthorized", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
}
