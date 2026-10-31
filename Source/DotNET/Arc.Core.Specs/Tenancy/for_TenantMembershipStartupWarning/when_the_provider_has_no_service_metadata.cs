// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy.for_TenantMembershipStartupWarning;

public class when_the_provider_has_no_service_metadata : Specification
{
    IServiceScope _scope;
    ILogger<TenantMembershipStartupWarning> _logger;
    Exception _exception;
    async Task Because()
    {
        var options = Options.Create(new ArcOptions());
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(ITenantIdResolver)).Returns(new HeaderTenantIdResolver(Substitute.For<IHttpRequestContextAccessor>(), options));
        var scopes = Substitute.For<IServiceScopeFactory>();
        services.GetService(typeof(IServiceScopeFactory)).Returns(scopes);
        _scope = Substitute.For<IServiceScope, IAsyncDisposable>();
        scopes.CreateScope().Returns(_scope);
        var scopedServices = Substitute.For<IServiceProvider>();
        scopedServices.GetService(typeof(IEnumerable<ITenantMembership>)).Returns(Array.Empty<ITenantMembership>());
        _scope.ServiceProvider.Returns(scopedServices);
        _logger = Substitute.For<ILogger<TenantMembershipStartupWarning>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        var warning = new TenantMembershipStartupWarning(services, options, _logger);
        _exception = await Catch.Exception(() => warning.StartAsync(TestContext.Current.CancellationToken));
    }
    [Fact] void should_complete_the_diagnostic() => _exception.ShouldBeNull();
    [Fact] void should_dispose_the_temporary_scope_asynchronously() => ((IAsyncDisposable)_scope).Received(1).DisposeAsync();
    [Fact] void should_warn_once() => _logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "Log").ShouldEqual(1);
}
