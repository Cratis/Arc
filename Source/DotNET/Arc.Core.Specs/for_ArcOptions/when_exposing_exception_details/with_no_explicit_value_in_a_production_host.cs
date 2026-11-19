// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.for_ArcOptions.when_exposing_exception_details;

public class with_no_explicit_value_in_a_production_host : given.arc_options_in_a_host
{
    protected override string EnvironmentName => "Production";

    void Because() => ResolveOptions();

    [Fact] void should_not_expose_the_details() => _exposed.ShouldBeFalse();
}
