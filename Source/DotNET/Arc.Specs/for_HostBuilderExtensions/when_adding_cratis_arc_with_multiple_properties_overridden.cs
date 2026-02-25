// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Identity;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Hosting.for_HostBuilderExtensions;

public class when_adding_cratis_arc_with_multiple_properties_overridden : Specification
{
    string _configuredHttpHeader = string.Empty;
    string _overrideHttpHeader = string.Empty;
    TenantResolverType _configuredTenantResolver;
    TenantResolverType _overrideTenantResolver;
    string _resolvedHttpHeader = string.Empty;
    TenantResolverType _resolvedTenantResolver;

    void Establish()
    {
        _configuredHttpHeader = "X-Correlation-From-Config";
        _overrideHttpHeader = "X-Correlation-From-Override";
        _configuredTenantResolver = TenantResolverType.Query;
        _overrideTenantResolver = TenantResolverType.Claim;
    }

    void Because()
    {
        var configurationPath = ConfigurationPath.Combine(HostBuilderExtensions.DefaultArcSectionPaths);
        var configurationValues = new Dictionary<string, string?>
        {
            [$"{configurationPath}:CorrelationId:HttpHeader"] = _configuredHttpHeader,
            [$"{configurationPath}:Tenancy:ResolverType"] = _configuredTenantResolver.ToString()
        };

        using var host = new HostBuilder()
            .ConfigureAppConfiguration((_, configurationBuilder) => configurationBuilder.AddInMemoryCollection(configurationValues))
            .AddCratisArc(options =>
            {
                options.IdentityDetailsProvider = typeof(DefaultIdentityDetailsProvider);
                options.CorrelationId.HttpHeader = _overrideHttpHeader;
                options.Tenancy.ResolverType = _overrideTenantResolver;
            })
            .Build();

        var resolvedOptions = host.Services.GetRequiredService<IOptions<ArcOptions>>();
        _resolvedHttpHeader = resolvedOptions.Value.CorrelationId.HttpHeader;
        _resolvedTenantResolver = resolvedOptions.Value.Tenancy.ResolverType;
    }

    [Fact] void should_use_overridden_correlation_header() => _resolvedHttpHeader.ShouldEqual(_overrideHttpHeader);
    [Fact] void should_use_overridden_tenant_resolver() => _resolvedTenantResolver.ShouldEqual(_overrideTenantResolver);
}
