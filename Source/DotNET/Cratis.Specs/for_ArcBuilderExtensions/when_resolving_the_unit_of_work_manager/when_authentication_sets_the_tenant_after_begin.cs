// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.AspNetCore.Http;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Cratis.Chronicle;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.for_ArcBuilderExtensions.when_resolving_the_unit_of_work_manager;

[Collection("UsesCurrentDirectory")]
public class when_authentication_sets_the_tenant_after_begin : given.a_chronicle_web_host
{
    int _resolutions;
    int _resolutionsBeforeAuthentication;
    EventStoreNamespaceName _namespace;

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc(
            configureOptions: options => options.Tenancy.ResolverType = TenantResolverType.Claim,
            configureBuilder: ConfigureChronicle);
        builder.Services.AddScoped<IEventStore>(services =>
        {
            _resolutions++;
            _namespace = services.GetRequiredService<IEventStoreNamespaceResolver>().Resolve();
            var store = Substitute.For<IEventStore>();
            store.GetEventSequence(EventSequenceId.Log).Returns(Substitute.For<IEventSequence>());
            return store;
        });
        _host = builder.Build();
    }

    async Task Because()
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpRequestContextAccessor>();
        await new HttpRequestContextMiddleware(accessor).InvokeAsync(context, _ =>
        {
            var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            using var unit = manager.Begin(Guid.NewGuid());
            _resolutionsBeforeAuthentication = _resolutions;
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("tenant_id", "authenticated-tenant")], "test"));
            unit.AddEvent(EventSequenceId.Log, "source", new object(), Causation.Unknown());
            return Task.CompletedTask;
        });
    }

    [Fact] void should_not_resolve_the_store_when_the_request_begins() => _resolutionsBeforeAuthentication.ShouldEqual(0);
    [Fact] void should_resolve_the_store_on_first_use() => _resolutions.ShouldEqual(1);
    [Fact] void should_bind_the_authenticated_tenant_namespace() => _namespace.Value.ShouldEqual("authenticated-tenant");
}
