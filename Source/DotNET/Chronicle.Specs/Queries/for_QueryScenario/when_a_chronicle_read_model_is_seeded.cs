// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Queries;
using Cratis.Arc.Queries;
using Cratis.Arc.Testing.Queries;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario;

public class when_a_chronicle_read_model_is_seeded : Specification
{
    readonly QueryScenario<QueryAccountBalance> _scenario = new();
    QueryResult _result = default!;

    void Establish()
    {
        var id = EventSourceId.New();
        _scenario.Given.ForEventSource(id).ReadModel(new QueryAccountBalance(42m));
        _id = id.Value;
    }

    string _id = string.Empty;

    async Task Because() => _result = await _scenario.Perform(nameof(QueryAccountBalance.ById), new QueryArguments { ["id"] = _id });

    [Fact] void should_resolve_the_seeded_read_model() => ((QueryAccountBalance)_result.Data).Balance.ShouldEqual(42m);
    [Fact] void should_succeed() => Assert.True(_result.IsSuccess, string.Join("; ", _result.ExceptionMessages));

    void Destroy() => _scenario.Dispose();
}
