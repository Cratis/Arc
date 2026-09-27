// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Testing.Queries;

namespace Cratis.Arc.Testing.for_QueryScenario;

public class when_performing_a_snapshot : Specification
{
    readonly QueryScenario<ScenarioReadModel> _scenario = new();
    QueryResult _result = default!;

    async Task Because() => _result = await _scenario.Perform(nameof(ScenarioReadModel.All));

    [Fact] void should_succeed() => Assert.True(_result.IsSuccess, string.Join("; ", _result.ExceptionMessages));
    [Fact] void should_return_the_read_model() => ((ScenarioReadModel)_result.Data).Name.ShouldEqual("All");

    void Destroy() => _scenario.Dispose();
}
