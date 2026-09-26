// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;
using Cratis.Arc.Queries;
using Cratis.Arc.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_OperationHttpContextAccessorRegistration;

public class when_a_plain_stream_emits_inside_another_selected_request
{
    [Fact]
    public void should_restore_the_subscriber_native_and_arc_identity_without_mutating_the_foreign_request()
    {
        var arc = new HttpRequestContextAccessor();
        var principals = new CurrentPrincipalAccessor(arc);
        var tenantResolver = Substitute.For<ITenantIdResolver>();
        var tenants = new TenantIdAccessor(tenantResolver);
        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        OperationHttpContextAccessorRegistration.Add(services);
        services.AddSingleton<IHttpRequestContextAccessor>(arc);
        services.AddSingleton(principals);
        services.AddSingleton(tenants);
        services.AddSingleton<IAuthorizationPolicyRuntime>(new AspNetAuthorizationPolicyRuntime(new ArcAuthorizationPolicyRuntime([])));
        using var provider = services.BuildServiceProvider();
        var native = (OperationHttpContextAccessor)provider.GetRequiredService<IHttpContextAccessor>();
        var subscriber = Principal("A");
        var producer = Principal("C");
        var original = new DefaultHttpContext { User = subscriber, RequestServices = provider };
        var foreignNative = new DefaultHttpContext { User = producer, RequestServices = provider };
        var subscriberRequest = Substitute.For<IHttpRequestContext>();
        subscriberRequest.User.Returns(subscriber);
        subscriberRequest.RequestServices.Returns(provider);
        var foreignRequest = Substitute.For<IHttpRequestContext>();
        foreignRequest.User.Returns(producer);
        foreignRequest.RequestServices.Returns(provider);
        native.HttpContext = original;
        var captured = ((IAuthorizationEmissionRuntime)provider.GetRequiredService<IAuthorizationPolicyRuntime>()).CaptureLiveRequest(provider);
        using (native.Begin(producer, provider, foreignNative))
        {
            arc.Current = foreignRequest;
            using (principals.UseAuthorizationPrincipal(producer, provider))
            using (tenants.Begin("tenant-C"))
            {
                using (ObservableEmissionIdentity.Begin(subscriberRequest, provider, subscriber, new TenantId("tenant-A"), captured, arc))
                {
                    principals.Current!.Identity!.Name.ShouldEqual("A");
                    tenants.Current.Value.ShouldEqual("tenant-A");
                    arc.Current.ShouldEqual(subscriberRequest);
                    native.HttpContext!.User.Identity!.Name.ShouldEqual("A");
                    native.HttpContext.RequestServices.ShouldEqual(provider);
                    original.User.Identity!.Name.ShouldEqual("A");
                    foreignNative.User.Identity!.Name.ShouldEqual("C");
                }
                principals.Current!.Identity!.Name.ShouldEqual("C");
                tenants.Current.Value.ShouldEqual("tenant-C");
                arc.Current.ShouldEqual(foreignRequest);
                native.HttpContext!.User.Identity!.Name.ShouldEqual("C");
            }
        }
        arc.Current = null;
        native.HttpContext = null;
    }

    static ClaimsPrincipal Principal(string name) => new(new ClaimsIdentity([new Claim(ClaimTypes.Name, name)], "test"));
}
