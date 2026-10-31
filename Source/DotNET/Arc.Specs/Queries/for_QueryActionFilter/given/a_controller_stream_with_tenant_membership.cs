// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Security.Claims;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_QueryActionFilter.given;

public class a_controller_stream_with_tenant_membership : a_controller_query_request
{
    protected async Task RunDeniedStream(bool webSocket)
    {
        var registrations = new ServiceCollection();
        registrations.AddLogging();
        registrations.AddOptions();
        registrations.AddCratisArcCore();
        var membership = Substitute.For<ITenantMembership>();
        membership.IsMember(Arg.Any<ClaimsPrincipal>(), Arg.Any<TenantId>()).Returns(false);
        registrations.AddScoped(_ => membership);
        await using var services = registrations.BuildServiceProvider();
        await using var scope = services.CreateAsyncScope();
        _httpContext.RequestServices = scope.ServiceProvider;
        var request = Substitute.For<IHttpRequestContext>();
        request.RequestServices.Returns(scope.ServiceProvider);
        request.Headers.Returns(new Dictionary<string, string> { ["x-cratis-tenant-id"] = "selected" });
        using var tenant = TenantIdAccessor.Independent.BeginRequest();
        using var context = HttpRequestContextAccessor.BeginRequest(services.GetRequiredService<IHttpRequestContextAccessor>(), request);
        _webSocketFeature.IsWebSocketRequest.Returns(webSocket);
        if (!webSocket) _httpContext.Request.Headers.Accept = "text/event-stream";
        using var subject = new ReplaySubject<TestReadModel>();
        subject.OnCompleted();
        _executed.Result = new ObjectResult(subject);
        await _filter.OnActionExecutionAsync(_actionContext, _next);
    }
}
