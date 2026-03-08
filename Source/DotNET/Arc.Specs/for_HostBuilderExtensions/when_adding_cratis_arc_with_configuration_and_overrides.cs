// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Hosting.for_HostBuilderExtensions;

public class when_adding_cratis_arc_with_configuration_and_overrides : Specification
{
    string _configuredHttpHeader = string.Empty;
    string _overrideHttpHeader = string.Empty;
    string _resolvedHttpHeader = string.Empty;

    void Establish()
    {
        _configuredHttpHeader = "X-Correlation-From-Config";
        _overrideHttpHeader = "X-Correlation-From-Override";
    }

    void Because()
    {
        var configurationPath = ConfigurationPath.Combine(HostBuilderExtensions.DefaultArcSectionPaths);
        var configurationValues = new Dictionary<string, string?>
        {
            [$"{configurationPath}:CorrelationId:HttpHeader"] = _configuredHttpHeader
        };

        using var host = new HostBuilder()
            .ConfigureAppConfiguration((_, configurationBuilder) => configurationBuilder.AddInMemoryCollection(configurationValues))
            .AddCratisArc(options =>
            {
                options.IdentityDetailsProvider = typeof(DefaultIdentityDetailsProvider);
                options.CorrelationId.HttpHeader = _overrideHttpHeader;
            })
            .Build();

        var options = host.Services.GetRequiredService<IOptions<ArcOptions>>();
        _resolvedHttpHeader = options.Value.CorrelationId.HttpHeader;
    }

    [Fact] void should_use_value_from_configure_options() => _resolvedHttpHeader.ShouldEqual(_overrideHttpHeader);
}
