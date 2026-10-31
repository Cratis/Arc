// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Tenancy.for_TenantMembershipStartupWarning.given;

public class a_host_with_tenancy : Specification
{
    protected ILogger _logger;
    protected TenantResolverType _resolver = TenantResolverType.Header;
    protected bool _ingress;
    protected bool _membership;
    protected bool _customResolver;
    protected bool _membershipFactoryThrows;
    protected bool _resolverFactoryThrows;
    protected Exception _exception;
    protected IHost _host;
    protected int Warnings => _logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "Log" && call.GetArguments()[0] is LogLevel.Warning);

    void Establish()
    {
        _logger = Substitute.For<ILogger<TenantMembershipStartupWarning>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
    }

    protected async Task StartHost()
    {
        _host = new HostBuilder()
            .UseDefaultServiceProvider(options => options.ValidateScopes = true)
            .AddCratisArcCore(options =>
            {
                options.Tenancy.ResolverType = _resolver;
                options.Tenancy.BaseDomain = "example.com";
                options.Tenancy.IngressValidatesTenant = _ingress;
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton(typeof(ILogger<TenantMembershipStartupWarning>), _logger);
                if (_membership)
                {
                    services.AddScoped(_ => _membershipFactoryThrows ? throw new InvalidOperationException("Not available at startup") : Substitute.For<ITenantMembership>());
                }
                if (_customResolver)
                {
                    services.AddSingleton(Substitute.For<ITenantIdResolver>());
                }
                if (_resolverFactoryThrows)
                {
                    services.AddSingleton<ITenantIdResolver>(_ => throw new InvalidOperationException("Unavailable"));
                }
            })
            .Build();
        _exception = await Catch.Exception(() => _host.StartAsync());
    }

    async Task Destroy()
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}
