// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.OptionsBinding;

public class when_leaving_websocket_origins_unconfigured : Specification
{
    [Theory]
    [InlineData(false, null)]
    [InlineData(true, null)]
    [InlineData(true, "")]
    void should_leave_origins_null_when_no_values_are_configured(bool includeSection, string? value)
    {
        var values = new Dictionary<string, string?>();
        if (includeSection)
        {
            values["Cratis:Arc:Query:AllowedWebSocketOrigins"] = value;
        }

        var options = new ArcOptions();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        ArcOptionsConfiguration.Bind(options, configuration.GetSection("Cratis:Arc"));

        options.Query.AllowedWebSocketOrigins.ShouldBeNull();
    }
}
