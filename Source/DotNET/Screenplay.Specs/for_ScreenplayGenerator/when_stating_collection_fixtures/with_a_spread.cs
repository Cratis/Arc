// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_stating_collection_fixtures;

public class with_a_spread : given.a_collection_fixture
{
    void Because() => GenerateFixture("string", "[Urgent, .. new[] { \"review\" }]");

    [Fact] void should_leave_out_the_scenario_rather_than_guess_the_spread() => AssertUnread();
}
