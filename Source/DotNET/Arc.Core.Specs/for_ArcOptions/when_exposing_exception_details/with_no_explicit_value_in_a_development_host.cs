// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.for_ArcOptions.when_exposing_exception_details;

public class with_no_explicit_value_in_a_development_host : given.arc_options_in_a_host
{
    protected override string EnvironmentName => "Development";

    void Because() => ResolveOptions();

    [Fact] void should_follow_the_process_environment_and_not_the_host() => _exposed.ShouldEqual(RuntimeEnvironment.IsDevelopment);
}
