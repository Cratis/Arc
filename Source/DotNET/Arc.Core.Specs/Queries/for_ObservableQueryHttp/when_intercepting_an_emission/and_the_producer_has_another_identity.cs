// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryHttp.when_intercepting_an_emission;

public class and_the_producer_has_another_identity : given.a_request
{
    ServiceProvider _root;
    IServiceScope _scope;
    TenantIdAccessor _tenantAccessor;
    CurrentPrincipalAccessor _principalAccessor;
    ClaimsPrincipal _selectedPrincipal;
    TenantId _interceptionTenant;
    ClaimsPrincipal _interceptionPrincipal;
    RequestValue _scopedValue;

    void Establish()
    {
        var resolver = Substitute.For<ITenantIdResolver>();
        resolver.Resolve().Returns("subscriber-tenant");
        _tenantAccessor = new TenantIdAccessor(resolver);
        _principalAccessor = new CurrentPrincipalAccessor(_accessor);
        _root = new ServiceCollection().AddSingleton(_accessor).AddSingleton(_interceptors).AddSingleton(_guards)
            .AddSingleton(_tenantAccessor).AddSingleton(_principalAccessor).AddScoped<RequestValue>()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        _scope = _root.CreateScope();
        _request.RequestServices.Returns(_scope.ServiceProvider);
        _selectedPrincipal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "selected-subscriber")]));
        _queryContext.AuthorizedPrincipal = _selectedPrincipal;
        _queryContext.EmissionTenant = new("subscriber-tenant");
        _interceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>()).Returns(call =>
        {
            _interceptionTenant = _tenantAccessor.Current;
            _interceptionPrincipal = _principalAccessor.Current!;
            _scopedValue = call.Arg<IServiceProvider>().GetRequiredService<RequestValue>();
            return Task.FromResult<IEnumerable<object>>(["intercepted"]);
        });
    }

    async Task Because()
    {
        using var subject = new Subject<string>();
        var response = ObservableQueryHttp.CreateResponse(_queryContext, subject, new(true, TimeSpan.FromSeconds(2)), _request);
        Task producer;
        using (ExecutionContext.SuppressFlow())
        {
            producer = Task.Run(() =>
            {
                using var tenant = _tenantAccessor.Begin(new("producer-tenant"));
                _accessor.Current = Substitute.For<IHttpRequestContext>();
                _accessor.Current.User = new ClaimsPrincipal();
                subject.OnNext("raw");
            });
        }
        await producer;
        _response = await response.WaitAsync(TimeSpan.FromSeconds(3));
    }

    void Destroy()
    {
        _scope.Dispose();
        _root.Dispose();
    }

    [Fact] void should_intercept_under_the_subscriber_tenant() => _interceptionTenant.Value.ShouldEqual("subscriber-tenant");
    [Fact] void should_intercept_under_the_selected_subscriber_principal() => _interceptionPrincipal.ShouldEqual(_selectedPrincipal);
    [Fact] void should_resolve_the_request_scoped_collaborator() => _scopedValue.ShouldEqual(_scope.ServiceProvider.GetRequiredService<RequestValue>());
    [Fact] void should_pass_the_selected_principal_to_the_guard() => _guardContexts.Single().Principal.ShouldEqual(_selectedPrincipal);

    public class RequestValue;
}
