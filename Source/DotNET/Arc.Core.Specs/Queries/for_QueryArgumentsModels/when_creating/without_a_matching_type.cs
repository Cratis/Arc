// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryArgumentsModels.when_creating;

/// <summary>
/// The overwhelming majority of queries have no arguments model, and resolving one for them would validate a shape
/// the developer never declared.
/// </summary>
public class without_a_matching_type : given.query_arguments_models
{
    bool _result;
    object _model;

    void Establish() => ForQuery("SearchBySomethingUnmodelled", new QueryParameter("term", typeof(string)));

    void Because() => _result = _models.TryCreateFor(_performer, ArgumentsOf(("term", "anything")), out _model);

    [Fact] void should_not_resolve_a_model() => _result.ShouldBeFalse();
    [Fact] void should_not_produce_an_instance() => _model.ShouldBeNull();
}
