// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_ArcOptions.when_exposing_exception_details;

public class with_explicit_false_configured_after_in_a_development_host : given.arc_options_in_a_host
{
    protected override string EnvironmentName => "Development";

    protected override void ConfigureAfter(IServiceCollection services) => services.Configure<ArcOptions>(_ => _.ExposeExceptionDetails = false);

    void Because() => ResolveOptions();

    [Fact] void should_keep_the_explicit_value() => _exposed.ShouldBeFalse();
}
