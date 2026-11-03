// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.OptionsBinding;

public class when_appending_configured_websocket_origins : Specification
{
    ArcOptions _options;
    IList<string> _origins;

    void Establish()
    {
        _origins = ["https://preconfigured.test"];
        _options = new ArcOptions();
        _options.Query.AllowedWebSocketOrigins = _origins;
    }

    void Because() => ArcOptionsConfiguration.Bind(
        _options,
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:Arc:Query:AllowedWebSocketOrigins:0"] = "https://configured.test"
        }).Build().GetSection("Cratis:Arc"));

    [Fact] void should_reuse_the_existing_list() => _options.Query.AllowedWebSocketOrigins.ShouldBeSame(_origins);

    [Fact]
    void should_append_the_configured_origin() =>
        _origins.ShouldEqual<string>(["https://preconfigured.test", "https://configured.test"]);
}
