// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_HostBuilderExtensions.when_adding_cratis_arc_core;

public class it_should_verify_tenant_membership_without_logging : Specification
{
    Exception _exception;

    void Because()
    {
        var registration = new ServiceCollection();
        registration.AddOptions();
        registration.AddCratisArcCore();
        var membership = Substitute.For<ITenantMembership>();
        membership.IsMember(Arg.Any<ClaimsPrincipal>(), Arg.Any<TenantId>()).Returns(false);
        registration.AddScoped(_ => membership);
        using var services = registration.BuildServiceProvider();
        using var scope = services.CreateScope();
        var request = Substitute.For<IHttpRequestContext>();
        request.RequestServices.Returns(scope.ServiceProvider);
        request.Headers.Returns(new Dictionary<string, string> { ["x-cratis-tenant-id"] = "selected" });
        using var tenant = TenantIdAccessor.Independent.BeginRequest();
        using var context = HttpRequestContextAccessor.BeginRequest(services.GetRequiredService<IHttpRequestContextAccessor>(), request);
        _exception = Catch.Exception(() => _ = services.GetRequiredService<ITenantIdAccessor>().Current);
    }

    [Fact] void should_enforce_membership_without_a_logger() => _exception.ShouldBeOfExactType<TenantMembershipDenied>();
}
