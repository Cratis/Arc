// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Testing.Queries;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_the_query_is_invalid : given.an_observable_query
{
    QueryObservation _observation = default!;

    async Task Because() => _observation = await _scenario.Observe(nameof(ObservableScenarioReadModel.Current), paging: new Paging(-1, 10, true));

    [Fact] void should_reject_the_initial_result() => _observation.Initial.IsValid.ShouldBeFalse();
    [Fact] void should_not_return_data() => _observation.Initial.Data.ShouldBeNull();
}
