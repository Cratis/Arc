// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryEmissionGuards.when_guarding;

public class with_no_guards : given.all_dependencies
{
    ObservableQueryEmissionVerdict _result;

    void Establish() => DiscoverGuards();

    async Task Because() => _result = await _guards.Guard(_context);

    [Fact] void should_not_report_having_guards() => _guards.HasGuards.ShouldBeFalse();
    [Fact] void should_allow_the_emission() => _result.ShouldEqual(ObservableQueryEmissionVerdict.Allow);
}
