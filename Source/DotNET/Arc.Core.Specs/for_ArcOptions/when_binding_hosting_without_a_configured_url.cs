// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.for_ArcOptions;

public class when_binding_hosting_without_a_configured_url : Specification
{
    ArcOptions _options;

    void Establish() => _options = new ArcOptions();

    void Because() => ArcOptionsConfiguration.Bind(
        _options,
        new ConfigurationBuilder().Build().GetSection("Cratis:Arc"));

    [Fact] void should_keep_the_default_application_url() => _options.Hosting.ApplicationUrl.ShouldEqual("http://+:5001/");
}
