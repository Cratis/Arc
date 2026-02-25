// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.for_ArcApplicationBuilderExtensions;

[Collection("UsesCurrentDirectory")]
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
        if (!Directory.Exists(Environment.CurrentDirectory))
        {
            Environment.CurrentDirectory = AppContext.BaseDirectory;
        }

        var configurationPath = ConfigurationPath.Combine(HostBuilderExtensions.DefaultSectionPaths);
        var configurationValues = new Dictionary<string, string?>
        {
            [$"{configurationPath}:CorrelationId:HttpHeader"] = _configuredHttpHeader
        };

        var builder = new ArcApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(configurationValues);
        builder.AddCratisArc(options =>
        {
            options.IdentityDetailsProvider = typeof(DefaultIdentityDetailsProvider);
            options.CorrelationId.HttpHeader = _overrideHttpHeader;
        });

        using var app = builder.Build();
        var options = app.Services.GetRequiredService<IOptions<ArcOptions>>();
        _resolvedHttpHeader = options.Value.CorrelationId.HttpHeader;
    }

    [Fact] void should_use_value_from_configure_options() => _resolvedHttpHeader.ShouldEqual(_overrideHttpHeader);
}
