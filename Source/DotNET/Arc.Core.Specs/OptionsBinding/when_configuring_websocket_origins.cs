// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.OptionsBinding;

public class when_configuring_websocket_origins : Specification
{
    ArcOptions _options;

    void Establish() => _options = new ArcOptions();

    void Because() => ArcOptionsConfiguration.Bind(
        _options,
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:Arc:Query:AllowedWebSocketOrigins:0"] = "https://trusted.test",
            ["Cratis:Arc:Query:AllowedWebSocketOrigins:1"] = "https://another.test"
        }).Build().GetSection("Cratis:Arc"));

    [Fact]
    void should_bind_both_origins() =>
        _options.Query.AllowedWebSocketOrigins.ShouldEqual<string>(["https://trusted.test", "https://another.test"]);
}
