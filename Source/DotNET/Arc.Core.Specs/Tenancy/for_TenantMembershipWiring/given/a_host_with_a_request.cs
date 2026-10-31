// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Tenancy.for_TenantMembershipWiring.given;

public class a_host_with_a_request : Specification
{
    protected bool _registerMembership = true;
    protected bool _allow;
    protected TenantId _result;
    protected Exception _exception;
    protected IHost _host;
    protected ITenantIdAccessor _accessor;
    protected ITenantScope _scope;
    protected TenantIdAccessor _concrete;
    protected ITenantMembership _membership;

    protected void ResolveTenant()
    {
        _membership = Substitute.For<ITenantMembership>();
        _membership.IsMember(Arg.Any<ClaimsPrincipal>(), Arg.Any<TenantId>()).Returns(_allow);
        _host = new HostBuilder()
            .UseDefaultServiceProvider(options => options.ValidateScopes = true)
            .AddCratisArcCore(options => options.UseHeaderTenancy())
            .ConfigureServices(services =>
            {
                if (_registerMembership)
                {
                    services.AddScoped(_ => _membership);
                }
            })
            .Build();
        _accessor = _host.Services.GetRequiredService<ITenantIdAccessor>();
        _scope = _host.Services.GetRequiredService<ITenantScope>();
        _concrete = _host.Services.GetRequiredService<TenantIdAccessor>();
        using var scope = _host.Services.CreateScope();
        var request = Substitute.For<IHttpRequestContext>();
        request.RequestServices.Returns(scope.ServiceProvider);
        request.Headers.Returns(new Dictionary<string, string> { ["x-cratis-tenant-id"] = "selected" });
        using var tenant = TenantIdAccessor.Independent.BeginRequest();
        using var context = HttpRequestContextAccessor.BeginRequest(_host.Services.GetRequiredService<IHttpRequestContextAccessor>(), request);
        _exception = Catch.Exception(() => _result = _accessor.Current);
    }

    void Destroy() => _host?.Dispose();
}
