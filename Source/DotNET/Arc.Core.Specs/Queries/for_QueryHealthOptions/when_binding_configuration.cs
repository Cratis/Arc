// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.Queries.for_QueryHealthOptions;

public class when_binding_configuration
{
    [Theory]
    [InlineData(null, false, true)]
    [InlineData("false", false, false)]
    [InlineData("true", true, true)]
    public void should_preserve_environment_defaults_unless_explicitly_overridden(string? value, bool developmentRequiresAuthentication, bool productionRequiresAuthentication)
    {
        var settings = new Dictionary<string, string?> { ["QueryHealth:Roles"] = null };
        if (value is not null)
        {
            settings["QueryHealth:RequireAuthentication"] = value;
        }
        var options = new ArcOptions();
        ArcOptionsConfiguration.Bind(options, new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        options.QueryHealth.RequiresAuthentication(isDevelopment: true).ShouldEqual(developmentRequiresAuthentication);
        options.QueryHealth.RequiresAuthentication(isDevelopment: false).ShouldEqual(productionRequiresAuthentication);
    }

    [Fact]
    public void should_keep_an_explicit_override_after_binding_an_unrelated_setting()
    {
        var options = new ArcOptions();
        options.QueryHealth.RequireAuthentication = true;
        ArcOptionsConfiguration.Bind(options, new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["QueryHealth:Roles"] = null }).Build());
        options.QueryHealth.RequiresAuthentication(isDevelopment: true).ShouldBeTrue();
    }
}
