// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Testing.Queries;

namespace Cratis.Arc.Testing.for_QueryScenario;

public class when_an_argument_is_missing : Specification
{
    readonly QueryScenario<ScenarioReadModel> _scenario = new();
    QueryResult _result = default!;

    async Task Because() => _result = await _scenario.Perform(nameof(ScenarioReadModel.ByName));

    [Fact] void should_report_validation_failure() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_not_run_the_query() => _result.Data.ShouldBeNull();

    void Destroy() => _scenario.Dispose();
}
