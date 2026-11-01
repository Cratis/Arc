// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.for_ArcOptions;

public class when_binding_hosting_from_configuration : Specification
{
    ArcOptions _options;

    void Establish() => _options = new ArcOptions();

    void Because() => ArcOptionsConfiguration.Bind(
        _options,
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cratis:Arc:Hosting:ApplicationUrl"] = "http://127.0.0.1:64321/"
            })
            .Build().GetSection("Cratis:Arc"));

    [Fact] void should_bind_the_application_url() => _options.Hosting.ApplicationUrl.ShouldEqual("http://127.0.0.1:64321/");
}
