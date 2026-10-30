// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_recovering_rendered_specifications;

public class with_a_complete_enum_event : given.a_recovered_command
{
    void Because() => GenerateScenario();
    [Fact] void should_recover_the_exact_captured_scenario() => AssertRecovered();
}
