// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Testing.Queries;

namespace Cratis.Arc.Testing.for_QueryScenario;

public class when_performing_with_a_concept_argument : Specification
{
    readonly QueryScenario<ScenarioReadModel> _scenario = new();
    QueryResult _result = default!;

    async Task Because() => _result = await _scenario.Perform(nameof(ScenarioReadModel.ByName), new QueryArguments { ["name"] = "Ada" });

    [Fact] void should_convert_and_pass_the_argument() => ((ScenarioReadModel)_result.Data).Name.ShouldEqual("Ada");
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();

    void Destroy() => _scenario.Dispose();
}
