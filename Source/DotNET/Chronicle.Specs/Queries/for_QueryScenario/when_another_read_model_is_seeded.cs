// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Queries;
using Cratis.Arc.Queries;
using Cratis.Arc.Testing.Queries;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario;

public class when_another_read_model_is_seeded : Specification
{
    readonly QueryScenario<QueryAccountBalance> _scenario = new();
    QueryResult _result = default!;
    string _id = string.Empty;

    void Establish()
    {
        var id = EventSourceId.New();
        _scenario.Given.ForEventSource(id).ReadModel(new OtherAccountBalance(23m));
        _id = id.Value;
    }

    async Task Because() => _result = await _scenario.Perform(nameof(QueryAccountBalance.FromOther), new QueryArguments { ["id"] = _id });

    [Fact] void should_resolve_the_other_read_model() => ((QueryAccountBalance)_result.Data).Balance.ShouldEqual(23m);

    void Destroy() => _scenario.Dispose();
}
