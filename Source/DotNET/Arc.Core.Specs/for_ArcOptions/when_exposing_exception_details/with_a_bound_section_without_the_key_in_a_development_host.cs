// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_ArcOptions.when_exposing_exception_details;

public class with_a_bound_section_without_the_key_in_a_development_host : given.arc_options_in_a_host
{
    protected override string EnvironmentName => "Development";

    protected override void ConfigureBefore(IServiceCollection services) =>
        ArcOptionsConfiguration.Configure(
            services,
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Cratis:Arc:ReadProtection:MaximumMode"] = "Measure" })
                .Build()
                .GetSection("Cratis:Arc"));

    void Because() => ResolveOptions();

    [Fact] void should_expose_the_details() => _exposed.ShouldBeTrue();
}
