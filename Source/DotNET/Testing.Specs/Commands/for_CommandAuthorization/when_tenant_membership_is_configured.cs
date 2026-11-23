// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Cratis.Arc.Testing.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_tenant_membership_is_configured : given.an_evaluation
{
    readonly HttpRequestContextAccessor _requests = new();
    ITenantMembership _membership;
    bool _pipelineDenied;

    void Establish()
    {
        _membership = Substitute.For<ITenantMembership>();
        _membership.IsMember(Arg.Any<ClaimsPrincipal>(), Arg.Any<TenantId>()).Returns(false);
        Services.AddSingleton(_membership);
        Services.AddSingleton<IHttpRequestContextAccessor>(_requests);
        Behavior.Evaluate = (context, cancellationToken) =>
        {
            _ = ((CommandContext)context.Resource).ServiceProvider.GetRequiredService<ITenantIdAccessor>().Current;
            return Task.FromResult(true);
        };
    }

    async Task Because()
    {
        Result = await CommandAuthorization.For(Provider, Principal, new given.ResourceCommand(true));
        await using var scenario = new CommandScenario<given.ResourceCommand>();
        foreach (var descriptor in Services) scenario.Services.Add(descriptor);
        scenario.WithPrincipal(Principal);
        var request = Substitute.For<IHttpRequestContext>();
        request.RequestServices.Returns(Provider);
        request.Headers.Returns(new Dictionary<string, string> { [new TenancyOptions().HttpHeader] = "spec-tenant" });
        request.User = Principal;

        // Real request membership verification is deliberately inert without an active request.
        // Add the request only for the pipeline run; the helper must diagnose the membership registration itself.
        _requests.Current = request;
        try { _pipelineDenied = !(await scenario.Execute(new given.ResourceCommand(true))).IsAuthorized; }
        finally { _requests.Current = null; }
    }

    [Fact] void should_be_unsupported() => Result.ShouldBeUnsupported();
    [Fact] void should_identify_membership() => Result.ReasonCode.ShouldEqual("tenant-membership");
    [Fact] void should_deny_the_pipeline_with_request_membership() => _pipelineDenied.ShouldBeTrue();
    [Fact] void should_verify_the_real_request_membership() => _membership.Received().IsMember(Arg.Any<ClaimsPrincipal>(), new TenantId("spec-tenant"));
    [Fact] void should_not_handle() => Behavior.HandlerCalls.ShouldEqual(0);
}
